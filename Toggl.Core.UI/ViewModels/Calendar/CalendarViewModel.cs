//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reactive;
//using System.Reactive.Disposables;
//using System.Reactive.Linq;
//using System.Reactive.Subjects;
//using System.Reactive.Threading.Tasks;
//using System.Threading.Tasks;
//using Toggl.Core.Analytics;
//using Toggl.Core.Calendar;
//using Toggl.Core.DataSources;
//using Toggl.Core.Extensions;
//using Toggl.Core.Interactors;
//using Toggl.Core.Models.Interfaces;
//using Toggl.Core.Services;
//using Toggl.Core.UI.Collections;
//using Toggl.Core.UI.Extensions;
//using Toggl.Core.UI.Navigation;
//using Toggl.Core.UI.Parameters;
//using Toggl.Core.UI.Services;
//using Toggl.Core.UI.Transformations;
//using Toggl.Core.UI.Views;
//using Toggl.Shared;
//using Toggl.Shared.Extensions;
//using Toggl.Storage.Settings;
//
//namespace Toggl.Core.UI.ViewModels.Calendar
//{
//    [Preserve(AllMembers = true)]
//    public sealed class CalendarViewModel : ViewModel
//    {
//        private readonly ITimeService timeService;
//        private readonly ITogglDataSource dataSource;
//        private readonly IUserPreferences userPreferences;
//        private readonly IAnalyticsService analyticsService;
//        private readonly IBackgroundService backgroundService;
//        private readonly IInteractorFactory interactorFactory;
//        private readonly IOnboardingStorage onboardingStorage;
//        private readonly ISchedulerProvider schedulerProvider;
//        private readonly IPermissionsChecker permissionsChecker;
//        private readonly IRxActionFactory rxActionFactory;
//
//        private readonly ISubject<bool> shouldShowOnboardingSubject;
//        private readonly ISubject<bool> hasCalendarsLinkedSubject;
//        private readonly ISubject<bool> calendarPermissionsOnViewAppearedSubject;
//        private readonly CompositeDisposable disposeBag = new CompositeDisposable();
//
//        public IObservable<bool> SettingsAreVisible { get; }
//
//        public IObservable<bool> ShouldShowOnboarding { get; }
//
//        public IObservable<bool> HasCalendarsLinked { get; }
//
//        public IObservable<TimeFormat> TimeOfDayFormat { get; }
//
//        public IObservable<string> TimeTrackedToday { get; }
//
//        public IObservable<string> CurrentDate { get; }
//
//        public UIAction GetStarted { get; }
//
//        public UIAction SkipOnboarding { get; }
//
//        public UIAction LinkCalendars { get; }
//
//        public UIAction SelectCalendars { get; }
//

//
//        public InputAction<DateTimeOffset> CreateTimeEntryAtOffset { get; }
//
//        public ObservableGroupedOrderedCollection<CalendarItem> CalendarItems { get; }
//
//        public CalendarViewModel(
//            ITogglDataSource dataSource,
//            ITimeService timeService,
//            IUserPreferences userPreferences,
//            IAnalyticsService analyticsService,
//            IBackgroundService backgroundService,
//            IInteractorFactory interactorFactory,
//            IOnboardingStorage onboardingStorage,
//            ISchedulerProvider schedulerProvider,
//            IPermissionsChecker permissionsChecker,
//            INavigationService navigationService,
//            IRxActionFactory rxActionFactory)
//            : base(navigationService)
//        {
//            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
//            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
//            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
//            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
//            Ensure.Argument.IsNotNull(backgroundService, nameof(backgroundService));
//            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
//            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
//            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
//            Ensure.Argument.IsNotNull(permissionsChecker, nameof(permissionsChecker));
//            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
//
//            this.dataSource = dataSource;
//            this.timeService = timeService;
//            this.userPreferences = userPreferences;
//            this.analyticsService = analyticsService;
//            this.backgroundService = backgroundService;
//            this.interactorFactory = interactorFactory;
//            this.onboardingStorage = onboardingStorage;
//            this.schedulerProvider = schedulerProvider;
//            this.permissionsChecker = permissionsChecker;
//            this.rxActionFactory = rxActionFactory;
//
//            var isCompleted = onboardingStorage.CompletedCalendarOnboarding();
//            shouldShowOnboardingSubject = new BehaviorSubject<bool>(!isCompleted);
//            hasCalendarsLinkedSubject = new BehaviorSubject<bool>(false);
//            calendarPermissionsOnViewAppearedSubject = new BehaviorSubject<bool>(false);
//
//            var onboardingObservable = shouldShowOnboardingSubject
//                .AsObservable()
//                .DistinctUntilChanged();
//
//            ShouldShowOnboarding = onboardingObservable.AsDriver(false, schedulerProvider);
//
//            var preferences = dataSource.Preferences.Current;
//
//            TimeOfDayFormat = preferences
//                .Select(current => current.TimeOfDayFormat)
//                .AsDriver(schedulerProvider);
//
//            var durationFormat = preferences.Select(current => current.DurationFormat);
//            var dateFormat = preferences.Select(current => current.DateFormat);
//            var timeTrackedToday = interactorFactory.ObserveTimeTrackedToday().Execute();
//
//            TimeTrackedToday = timeTrackedToday
//                .StartWith(TimeSpan.Zero)
//                .CombineLatest(durationFormat, DurationAndFormatToString.Convert)
//                .AsDriver(schedulerProvider);
//
//            CurrentDate = timeService.CurrentDateTimeObservable
//                .Select(dateTime => dateTime.ToLocalTime().Date)
//                .DistinctUntilChanged()
//                .CombineLatest(dateFormat, (date, format) => DateTimeToFormattedString.Convert(date, format.Long))
//                .AsDriver(schedulerProvider);
//
//            GetStarted = rxActionFactory.FromAsync(getStarted);
//            SkipOnboarding = rxActionFactory.FromAction(skipOnboarding);
//            LinkCalendars = rxActionFactory.FromAsync(() => linkCalendars(false));


//
//            SettingsAreVisible = onboardingObservable
//                .SelectMany(_ => permissionsChecker.CalendarPermissionGranted)
//                .DistinctUntilChanged();
//
//            HasCalendarsLinked = userPreferences.EnabledCalendars.CombineLatest(
//                    permissionsChecker.CalendarPermissionGranted,
//                    hasCalendarsLinkedSubject.AsObservable(),
//                    calendarPermissionsOnViewAppearedSubject,
//                    hasCalendarsLinked)
//                .DistinctUntilChanged();
//
//            SelectCalendars = rxActionFactory.FromAsync(() => selectUserCalendars(false), SettingsAreVisible);
//
//
//            CreateTimeEntryAtOffset = rxActionFactory.FromAsync<DateTimeOffset>(createTimeEntryAtOffset);
//
//        }
//
//        public void Init(string eventId)
//        {
//            if (string.IsNullOrWhiteSpace(eventId))
//                return;
//
//            interactorFactory
//                .GetCalendarItemWithId(eventId)
//                .Execute()
//                .SelectMany(calendarItem =>
//                    handleCalendarItem(calendarItem).ToObservable())
//                .Subscribe()
//                .DisposedBy(disposeBag);
//        }
//
//        public override Task Initialize()
//        {
//            var dayChangedObservable = timeService
//                .MidnightObservable
//                .SelectUnit();
//
//
//
//            selectedCalendarsChangedObservable
//                .Merge(dayChangedObservable)
//                .Merge(appResumedFromBackgroundObservable)
//                .StartWith(Unit.Default)
//                .CombineLatest(userPreferences.CalendarNotificationsEnabled, CommonFunctions.Second)
//                .SelectMany(refreshNotifications)
//                .Subscribe()
//                .DisposedBy(disposeBag);
//
//            return base.Initialize();
//        }
//
//        public override void ViewAppeared()
//        {
//            base.ViewAppeared();
//            checkCalendarPermissions();
//        }
//
//        private async Task checkCalendarPermissions()
//        {
//            var authorized = await permissionsChecker.CalendarPermissionGranted;
//            calendarPermissionsOnViewAppearedSubject.OnNext(authorized);
//        }
//
//        private bool hasCalendarsLinked(List<string> calendars, bool hasInitialCalendarPermissions, bool didLinkCalendars, bool hadPermissionsWhenAppeared)
//            => calendars != null
//               && calendars.Count > 0
//               && (hasInitialCalendarPermissions || didLinkCalendars || hadPermissionsWhenAppeared);
//
//        private IObservable<Unit> refreshNotifications(bool notificationsAreEnabled)
//            => Observable.FromAsync(async () =>
//            {
//                await interactorFactory.UnscheduleAllNotifications().Execute();
//
//                if (!notificationsAreEnabled)
//                    return;
//
//                await interactorFactory.ScheduleEventNotificationsForNextWeek().Execute();
//            });
//
//        private async Task getStarted()
//        {
//            analyticsService.CalendarOnboardingStarted.Track();
//            await linkCalendars(true);
//
//            onboardingStorage.SetCompletedCalendarOnboarding();
//            shouldShowOnboardingSubject.OnNext(false);
//        }
//
//        private async Task linkCalendars(bool isOnboarding)
//        {
//            var calendarPermissionGranted = await View.RequestCalendarAuthorization();
//            hasCalendarsLinkedSubject.OnNext(calendarPermissionGranted);
//            if (calendarPermissionGranted)
//            {
//                await selectUserCalendars(isOnboarding);
//                var notificationPermissionGranted = await View.RequestNotificationAuthorization();
//                userPreferences.SetCalendarNotificationsEnabled(notificationPermissionGranted);
//            }
//            else
//            {
//                await Navigate<CalendarPermissionDeniedViewModel, Unit>();
//            }
//        }
//
//        private void skipOnboarding()
//        {
//            onboardingStorage.SetCompletedCalendarOnboarding();
//            shouldShowOnboardingSubject.OnNext(false);
//        }
//
//        private async Task selectUserCalendars(bool isOnboarding)
//        {
//            var calendarsExist = await interactorFactory
//                .GetUserCalendars()
//                .Execute()
//                .Select(calendars => calendars.Any());
//
//            if (calendarsExist)
//            {
//                var calendarIds = await Navigate<SelectUserCalendarsViewModel, bool, string[]>(isOnboarding);
//
//                interactorFactory.SetEnabledCalendars(calendarIds).Execute();
//            }
//            else if (!isOnboarding)
//            {
//                await View.Alert(Resources.Oops, Resources.NoCalendarsFoundMessage, Resources.Ok);
//            }
//        }
//
//        private async Task handleCalendarEventLongPressed(CalendarItem calendarItem)
//        {
//            var runningStartedNow =
//                calendarItem
//                    .WithStartTime(timeService.CurrentDateTime)
//                    .WithDuration(null);
//
//            var options = new List<SelectOption<CalendarItem?>>
//            {
//                new SelectOption<CalendarItem?>(calendarItem, Resources.CalendarCopyEventToTimeEntry),
//                new SelectOption<CalendarItem?>(runningStartedNow, Resources.CalendarStartNow)
//            };
//
//            if (timeService.CurrentDateTime >= calendarItem.StartTime)
//            {
//                var runningStartingAtTheEventStart = calendarItem.WithDuration(null);
//                var option = new SelectOption<CalendarItem?>(runningStartingAtTheEventStart, Resources.CalendarStartWhenTheEventStarts);
//                options.Add(option);
//            }
//
//            var selectedOption = await View.SelectAction(Resources.CalendarWhatToDoWithCalendarEvent, options);
//            if (selectedOption.HasValue)
//            {
//                await createTimeEntryFromCalendarItem(selectedOption.Value);
//            }
//        }
//

//
//        private async Task createTimeEntryAtOffset(DateTimeOffset startTime)
//        {
//            var startParams = StartTimeEntryParameters
//                .ForCalendarTapAndDrag(startTime);
//
//            await Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(startParams);
//        }
//
//        private async Task updateTimeEntry(CalendarItem calendarItem)
//        {
//            if (!calendarItem.IsEditable() || calendarItem.TimeEntryId == null)
//                return;
//
//            var timeEntry = await interactorFactory.GetTimeEntryById(calendarItem.TimeEntryId.Value).Execute();
//
//            var dto = new DTOs.EditTimeEntryDto
//            {
//                Id = timeEntry.Id,
//                Description = timeEntry.Description,
//                StartTime = calendarItem.StartTime,
//                StopTime = calendarItem.EndTime,
//                ProjectId = timeEntry.ProjectId,
//                TaskId = timeEntry.TaskId,
//                Billable = timeEntry.Billable,
//                WorkspaceId = timeEntry.WorkspaceId,
//                TagIds = timeEntry.TagIds
//            };
//
//            var duration = calendarItem.Duration.HasValue
//                ? calendarItem.Duration.Value.TotalSeconds
//                : (timeService.CurrentDateTime - calendarItem.StartTime.LocalDateTime).TotalSeconds;
//
//            if (timeEntry.Duration != duration)
//            {
//                analyticsService.TimeEntryChangedFromCalendar.Track(CalendarChangeEvent.Duration);
//            }
//
//            if (timeEntry.Start != calendarItem.StartTime)
//            {
//                analyticsService.TimeEntryChangedFromCalendar.Track(CalendarChangeEvent.StartTime);
//            }
//
//            await interactorFactory.UpdateTimeEntry(dto).Execute();
//        }
//    }
//}
