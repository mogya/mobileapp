using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Core.Analytics;
using Toggl.Core.Calendar;
using Toggl.Core.DataSources;
using Toggl.Core.Extensions;
using Toggl.Core.Interactors;
using Toggl.Core.Models.Interfaces;
using Toggl.Core.Services;
using Toggl.Core.UI.Collections;
using Toggl.Core.UI.Navigation;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Toggl.Storage.Settings;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.Views;

namespace Toggl.Core.UI.ViewModels.Calendar
{
    [Preserve(AllMembers = true)]
    public sealed class CalendarDayViewModel : ViewModel
    {
        private readonly ITimeService timeService;
        private readonly IAnalyticsService analyticsService;
        private readonly IInteractorFactory interactorFactory;

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        public DateTimeOffset Date { get; }

        public ObservableGroupedOrderedCollection<CalendarItem> CalendarItems { get; }

        public IObservable<TimeFormat> TimeOfDayFormat { get; }

        public InputAction<CalendarItem> OnItemTapped { get; }
        public InputAction<CalendarItem> OnTimeEntryEdited { get; }
        public InputAction<CalendarItem> OnCalendarEventLongPressed { get; }
        public InputAction<(DateTimeOffset, TimeSpan)> OnDurationSelected { get; }

        public CalendarDayViewModel(
            DateTimeOffset date,
            ITimeService timeService,
            ITogglDataSource dataSource,
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
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(backgroundService, nameof(backgroundService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));

            Date = date;
            this.timeService = timeService;
            this.analyticsService = analyticsService;
            this.interactorFactory = interactorFactory;

            OnItemTapped = rxActionFactory.FromAsync<CalendarItem>(handleCalendarItem);
            OnTimeEntryEdited = rxActionFactory.FromAsync<CalendarItem>(onTimeEntryEdited);
            OnCalendarEventLongPressed = rxActionFactory.FromAsync<CalendarItem>(handleCalendarEventLongPressed);
            OnDurationSelected = rxActionFactory.FromAsync<(DateTimeOffset StartTime, TimeSpan Duration)>(
                tuple => durationSelected(tuple.StartTime, tuple.Duration));

            var preferences = dataSource.Preferences.Current;
            TimeOfDayFormat = preferences
                .Select(current => current.TimeOfDayFormat)
                .AsDriver(schedulerProvider);

            CalendarItems = new ObservableGroupedOrderedCollection<CalendarItem>(
                indexKey: item => item.StartTime,
                orderingKey: item => item.StartTime,
                groupingKey: _ => 0);

            var selectedCalendarsChanged = userPreferences
                .EnabledCalendars
                .SelectUnit();

            var appResumedFromBackground = backgroundService
                .AppResumedFromBackground
                .SelectUnit();

            Observable.Merge(dataSource.TimeEntries.ItemsChanged, selectedCalendarsChanged, appResumedFromBackground)
                .ObserveOn(schedulerProvider.BackgroundScheduler)
                .SelectMany(_ => reloadData())
                .ObserveOn(schedulerProvider.MainScheduler)
                .Subscribe(CalendarItems.ReplaceWith)
                .DisposedBy(disposeBag);
        }

        private IObservable<IEnumerable<CalendarItem>> reloadData()
        {
            return interactorFactory.GetCalendarItemsForDate(Date.ToLocalTime().Date).Execute();
        }

        private async Task handleCalendarItem(CalendarItem calendarItem)
        {
            switch (calendarItem.Source)
            {
                case CalendarItemSource.TimeEntry when calendarItem.TimeEntryId.HasValue:
                    analyticsService.EditViewOpenedFromCalendar.Track();
                    await Navigate<EditTimeEntryViewModel, long[]>(new[] { calendarItem.TimeEntryId.Value });
                    break;

                case CalendarItemSource.Calendar:
                    await createTimeEntryFromCalendarItem(calendarItem);
                    break;
            }
        }

        private async Task createTimeEntryFromCalendarItem(CalendarItem calendarItem)
        {
            var workspace = await interactorFactory.GetDefaultWorkspace()
                .TrackException<InvalidOperationException, IThreadSafeWorkspace>("CalendarViewModel.handleCalendarItem")
                .Execute();
            var prototype = calendarItem.AsTimeEntryPrototype(workspace.Id);
            await interactorFactory.CreateTimeEntry(prototype, TimeEntryStartOrigin.CalendarEvent).Execute();
        }

        private async Task onTimeEntryEdited(CalendarItem calendarItem)
        {
            if (!calendarItem.IsEditable() || calendarItem.TimeEntryId == null)
                return;

            var timeEntry = await interactorFactory.GetTimeEntryById(calendarItem.TimeEntryId.Value).Execute();

            var dto = new DTOs.EditTimeEntryDto
            {
                Id = timeEntry.Id,
                Description = timeEntry.Description,
                StartTime = calendarItem.StartTime,
                StopTime = calendarItem.EndTime,
                ProjectId = timeEntry.ProjectId,
                TaskId = timeEntry.TaskId,
                Billable = timeEntry.Billable,
                WorkspaceId = timeEntry.WorkspaceId,
                TagIds = timeEntry.TagIds
            };

            var duration = calendarItem.Duration.HasValue
                ? calendarItem.Duration.Value.TotalSeconds
                : (timeService.CurrentDateTime - calendarItem.StartTime.LocalDateTime).TotalSeconds;

            if (timeEntry.Duration != duration)
            {
                analyticsService.TimeEntryChangedFromCalendar.Track(CalendarChangeEvent.Duration);
            }

            if (timeEntry.Start != calendarItem.StartTime)
            {
                analyticsService.TimeEntryChangedFromCalendar.Track(CalendarChangeEvent.StartTime);
            }

            await interactorFactory.UpdateTimeEntry(dto).Execute();
        }

        private async Task handleCalendarEventLongPressed(CalendarItem calendarItem)
        {
            var runningStartedNow =
                calendarItem
                    .WithStartTime(timeService.CurrentDateTime)
                    .WithDuration(null);

            var options = new List<SelectOption<CalendarItem?>>
            {
                new SelectOption<CalendarItem?>(calendarItem, Resources.CalendarCopyEventToTimeEntry),
                new SelectOption<CalendarItem?>(runningStartedNow, Resources.CalendarStartNow)
            };

            if (timeService.CurrentDateTime >= calendarItem.StartTime)
            {
                var runningStartingAtTheEventStart = calendarItem.WithDuration(null);
                var option = new SelectOption<CalendarItem?>(runningStartingAtTheEventStart, Resources.CalendarStartWhenTheEventStarts);
                options.Add(option);
            }

            var selectedOption = await View.SelectAction(Resources.CalendarWhatToDoWithCalendarEvent, options);
            if (selectedOption.HasValue)
            {
                await createTimeEntryFromCalendarItem(selectedOption.Value);
            }
        }

        private async Task durationSelected(DateTimeOffset startTime, TimeSpan duration)
        {
            var workspace = await interactorFactory.GetDefaultWorkspace()
                .TrackException<InvalidOperationException, IThreadSafeWorkspace>("CalendarViewModel.durationSelected")
                .Execute();

            var prototype = duration.AsTimeEntryPrototype(startTime, workspace.Id);
            var timeEntry = await interactorFactory.CreateTimeEntry(prototype, TimeEntryStartOrigin.CalendarTapAndDrag).Execute();

            await Navigate<EditTimeEntryViewModel, long[]>(new[] { timeEntry.Id });
        }
    }
}
