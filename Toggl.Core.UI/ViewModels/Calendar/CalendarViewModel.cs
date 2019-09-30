using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Core.Analytics;
using Toggl.Core.DataSources;
using Toggl.Core.Interactors;
using Toggl.Core.Services;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.Navigation;
using Toggl.Core.UI.Transformations;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Toggl.Shared.Extensions.Reactive;
using Toggl.Storage.Settings;

namespace Toggl.Core.UI.ViewModels.Calendar
{
    [Preserve(AllMembers = true)]
    public sealed class CalendarViewModel : ViewModel
    {
        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IUserPreferences userPreferences;
        private readonly IAnalyticsService analyticsService;
        private readonly IBackgroundService backgroundService;
        private readonly IInteractorFactory interactorFactory;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IRxActionFactory rxActionFactory;

        public IObservable<string> CurrentlyShownDateString { get; }

        public UIAction OpenSettings { get; }

        public BehaviorRelay<int> CurrentlyVisiblePage { get; }

        public CalendarViewModel(
            ITogglDataSource dataSource,
            ITimeService timeService,
            IRxActionFactory rxActionFactory,
            IUserPreferences userPreferences,
            IAnalyticsService analyticsService,
            IBackgroundService backgroundService,
            IInteractorFactory interactorFactory,
            ISchedulerProvider schedulerProvider,
            INavigationService navigationService)
            : base(navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(backgroundService, nameof(backgroundService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));

            this.dataSource = dataSource;
            this.timeService = timeService;
            this.rxActionFactory = rxActionFactory;
            this.userPreferences = userPreferences;
            this.analyticsService = analyticsService;
            this.backgroundService = backgroundService;
            this.interactorFactory = interactorFactory;
            this.schedulerProvider = schedulerProvider;

            OpenSettings = rxActionFactory.FromAsync(openSettings);

            CurrentlyVisiblePage = new BehaviorRelay<int>(0);

            var dateFormatObservable = dataSource.Preferences.Current
                .Select(current => current.DateFormat);
            CurrentlyShownDateString = CurrentlyVisiblePage.AsObservable()
                .Select(pageIndexToDate)
                .DistinctUntilChanged()
                .CombineLatest(
                    dateFormatObservable,
                    (date, dateFormat) => DateTimeToFormattedString.Convert(date, dateFormat.Long))
                .AsDriver(schedulerProvider);
        }

        public CalendarDayViewModel DayViewModelFor(DateTimeOffset date)
            => new CalendarDayViewModel(
                date,
                timeService,
                dataSource,
                rxActionFactory,
                userPreferences,
                analyticsService,
                backgroundService,
                interactorFactory,
                schedulerProvider,
                NavigationService);

        private DateTimeOffset pageIndexToDate(int index)
            => timeService.CurrentDateTime.ToLocalTime().Date.AddDays(index);

        private Task openSettings()
            => Navigate<SettingsViewModel>();
    }
}
