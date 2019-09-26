using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Core.Calendar;
using Toggl.Core.DataSources;
using Toggl.Core.Extensions;
using Toggl.Core.Interactors;
using Toggl.Core.Services;
using Toggl.Core.UI.Collections;
using Toggl.Core.UI.Navigation;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Toggl.Storage.Settings;
using Toggl.Core.UI.Extensions;

namespace Toggl.Core.UI.ViewModels.Calendar
{
    [Preserve(AllMembers = true)]
    public sealed class CalendarDayViewModel : ViewModel
    {
        private readonly ITogglDataSource dataSource;
        private readonly IUserPreferences userPreferences;
        private readonly IInteractorFactory interactorFactory;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IBackgroundService backgroundService;

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        public DateTimeOffset Date { get; }

        public ObservableGroupedOrderedCollection<CalendarItem> CalendarItems { get; }

        public IObservable<TimeFormat> TimeOfDayFormat { get; }

        public CalendarDayViewModel(
            DateTimeOffset date,
            ITogglDataSource dataSource,
            IUserPreferences userPreferences,
            IBackgroundService backgroundService,
            ISchedulerProvider schedulerProvider,
            IInteractorFactory interactorFactory,
            INavigationService navigationService)
            : base(navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(backgroundService, nameof(backgroundService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));

            this.Date = date;
            this.dataSource = dataSource;
            this.userPreferences = userPreferences;
            this.backgroundService = backgroundService;
            this.schedulerProvider = schedulerProvider;
            this.interactorFactory = interactorFactory;

            var preferences = dataSource.Preferences.Current;

            TimeOfDayFormat = preferences
                .Select(current => current.TimeOfDayFormat)
                .AsDriver(schedulerProvider);

            CalendarItems = new ObservableGroupedOrderedCollection<CalendarItem>(
                indexKey: item => item.StartTime,
                orderingKey: item => item.StartTime,
                groupingKey: _ => 0);

            var selectedCalendarsChangedObservable = userPreferences
                .EnabledCalendars
                .SelectUnit();

            var appResumedFromBackgroundObservable = backgroundService
                .AppResumedFromBackground
                .SelectUnit();

            dataSource.TimeEntries
                .ItemsChanged()
                .Merge(selectedCalendarsChangedObservable)
                .Merge(appResumedFromBackgroundObservable)
                .SubscribeOn(schedulerProvider.BackgroundScheduler)
                .ObserveOn(schedulerProvider.BackgroundScheduler)
                .SelectMany(_ => reloadData())
                .Subscribe(CalendarItems.ReplaceWith)
                .DisposedBy(disposeBag);
        }

        //public override async Task Initialize()
        //{
        //    await base.Initialize();

        //    var selectedCalendarsChangedObservable = userPreferences
        //        .EnabledCalendars
        //        .SelectUnit();

        //    var appResumedFromBackgroundObservable = backgroundService
        //        .AppResumedFromBackground
        //        .SelectUnit();

        //    dataSource.TimeEntries
        //        .ItemsChanged()
        //        .Merge(selectedCalendarsChangedObservable)
        //        .Merge(appResumedFromBackgroundObservable)
        //        .SubscribeOn(schedulerProvider.BackgroundScheduler)
        //        .ObserveOn(schedulerProvider.BackgroundScheduler)
        //        .SelectMany(_ => reloadData())
        //        .Subscribe(CalendarItems.ReplaceWith)
        //        .DisposedBy(disposeBag);
        //}

        private IObservable<IEnumerable<CalendarItem>> reloadData()
        {
            return interactorFactory.GetCalendarItemsForDate(Date.ToLocalTime().Date).Execute();
        }
    }
}
