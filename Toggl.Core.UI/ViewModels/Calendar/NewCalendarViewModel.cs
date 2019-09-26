using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Toggl.Core.Analytics;
using Toggl.Core.DataSources;
using Toggl.Core.Interactors;
using Toggl.Core.Services;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.Navigation;
using Toggl.Core.UI.Services;
using Toggl.Core.UI.Transformations;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Toggl.Storage.Settings;

namespace Toggl.Core.UI.ViewModels.Calendar
{
    [Preserve(AllMembers = true)]
    public sealed class NewCalendarViewModel : ViewModel
    {
        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IUserPreferences userPreferences;
        private readonly IAnalyticsService analyticsService;
        private readonly IBackgroundService backgroundService;
        private readonly IInteractorFactory interactorFactory;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IPermissionsChecker permissionsChecker;
        private readonly IRxActionFactory rxActionFactory;

        public IObservable<TimeFormat> TimeOfDayFormat { get; }

        public IObservable<string> DailyTrackedTime { get; }

        public IObservable<string> CurrentlyShownDate { get; }

        public UIAction SelectCalendars { get; }

        public InputAction<DateTimeOffset> UpdateCurrentlyShownDate { get; }

        public NewCalendarViewModel(
            ITogglDataSource dataSource,
            ITimeService timeService,
            IRxActionFactory rxActionFactory,
            IUserPreferences userPreferences,
            IAnalyticsService analyticsService,
            IBackgroundService backgroundService,
            IInteractorFactory interactorFactory,
            IOnboardingStorage onboardingStorage,
            ISchedulerProvider schedulerProvider,
            INavigationService navigationService,
            IPermissionsChecker permissionsChecker)
            : base(navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(backgroundService, nameof(backgroundService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(permissionsChecker, nameof(permissionsChecker));

            this.dataSource = dataSource;
            this.timeService = timeService;
            this.rxActionFactory = rxActionFactory;
            this.userPreferences = userPreferences;
            this.analyticsService = analyticsService;
            this.backgroundService = backgroundService;
            this.interactorFactory = interactorFactory;
            this.onboardingStorage = onboardingStorage;
            this.schedulerProvider = schedulerProvider;
            this.permissionsChecker = permissionsChecker;

            SelectCalendars = rxActionFactory.FromAsync(linkCalendars);
            UpdateCurrentlyShownDate = rxActionFactory.FromAction((DateTimeOffset _) => { });

            var preferences = dataSource.Preferences.Current;

            var currentlyShownDateObservable = UpdateCurrentlyShownDate.Inputs
                .StartWith(timeService.CurrentDateTime);

            var dateFormatObservable = preferences.Select(current => current.DateFormat);
            CurrentlyShownDate = currentlyShownDateObservable
                .Select(dateTime => dateTime.ToLocalTime().Date)
                .DistinctUntilChanged()
                .CombineLatest(
                    dateFormatObservable,
                    (date, dateFormat) => DateTimeToFormattedString.Convert(date, dateFormat.Long))
                .AsDriver(schedulerProvider);

            var durationFormat = preferences.Select(current => current.DurationFormat);
            currentlyShownDateObservable
                .CombineLatest(
                    durationFormat,

                    )

            TimeOfDayFormat = preferences
                .Select(current => current.TimeOfDayFormat)
                .AsDriver(schedulerProvider);

//            var durationFormat = preferences.Select(current => current.DurationFormat);
//            var timeTrackedToday = interactorFactory.ObserveTimeTrackedToday().Execute();

//            DailyTrackedTime = timeTrackedToday
//                .StartWith(TimeSpan.Zero)
//                .CombineLatest(durationFormat, DurationAndFormatToString.Convert)
//                .AsDriver(schedulerProvider);
        }

        public CalendarDayViewModel DayViewModelFor(DateTimeOffset date)
            => new CalendarDayViewModel(
                date,
                dataSource,
                userPreferences,
                backgroundService,
                schedulerProvider,
                interactorFactory,
                NavigationService);

        private async Task selectUserCalendars()
        {
            var calendarsExist = await interactorFactory
                .GetUserCalendars()
                .Execute()
                .Select(calendars => calendars.Any());

            if (calendarsExist)
            {
                var calendarIds = await Navigate<SelectUserCalendarsViewModel, bool, string[]>(false);

                interactorFactory.SetEnabledCalendars(calendarIds).Execute();
            }
            else
            {
                await View.Alert(Resources.Oops, Resources.NoCalendarsFoundMessage, Resources.Ok);
            }
        }

        private async Task linkCalendars()
        {
            var calendarPermissionGranted = await View.RequestCalendarAuthorization();
            if (calendarPermissionGranted)
            {
                await selectUserCalendars();
                var notificationPermissionGranted = await View.RequestNotificationAuthorization();
                userPreferences.SetCalendarNotificationsEnabled(notificationPermissionGranted);
            }
            else
            {
                await Navigate<CalendarPermissionDeniedViewModel, Unit>();
            }
        }
    }
}
