using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using NSubstitute;
using Toggl.Core.Analytics;
using Toggl.Core.Calendar;
using Toggl.Core.DataSources;
using Toggl.Core.DTOs;
using Toggl.Core.Interactors;
using Toggl.Core.Models;
using Toggl.Core.Models.Interfaces;
using Toggl.Core.Tests.Generators;
using Toggl.Core.Tests.Mocks;
using Toggl.Core.UI.Navigation;
using Toggl.Core.UI.ViewModels;
using Toggl.Core.UI.ViewModels.Calendar;
using Toggl.Core.UI.Views;
using Toggl.Shared;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Toggl.Core.Tests.UI.ViewModels
{
    public sealed class CalendarDayViewModelTests
    {
        public abstract class CalendarDayViewModelTest : BaseViewModelTests<CalendarDayViewModel>
        {
            protected const long TimeEntryId = 10;
            protected const long DefaultWorkspaceId = 1;
            protected IInteractor<IObservable<IEnumerable<CalendarItem>>> CalendarInteractor { get; }

            protected static DateTimeOffset Now { get; } = new DateTimeOffset(2018, 8, 10, 12, 0, 0, TimeSpan.Zero);

            protected CalendarDayViewModelTest()
            {
                CalendarInteractor = Substitute.For<IInteractor<IObservable<IEnumerable<CalendarItem>>>>();

                var workspace = new MockWorkspace { Id = DefaultWorkspaceId };
                var timeEntry = new MockTimeEntry { Id = TimeEntryId };

                TimeService.CurrentDateTime.Returns(Now);

                InteractorFactory
                    .GetCalendarItemsForDate(Arg.Any<DateTime>())
                    .Returns(CalendarInteractor);

                InteractorFactory
                    .GetDefaultWorkspace()
                    .Execute()
                    .Returns(Observable.Return(workspace));

                InteractorFactory
                    .CreateTimeEntry(Arg.Any<ITimeEntryPrototype>(), TimeEntryStartOrigin.CalendarEvent)
                    .Execute()
                    .Returns(Observable.Return(timeEntry));

                InteractorFactory
                    .CreateTimeEntry(Arg.Any<ITimeEntryPrototype>(), TimeEntryStartOrigin.CalendarTapAndDrag)
                    .Execute()
                    .Returns(Observable.Return(timeEntry));

                InteractorFactory
                    .UpdateTimeEntry(Arg.Any<EditTimeEntryDto>())
                    .Execute()
                    .Returns(Observable.Return(timeEntry));
            }

            protected override CalendarDayViewModel CreateViewModel()
                => new CalendarDayViewModel(
                    new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero),
                    TimeService,
                    DataSource,
                    RxActionFactory,
                    UserPreferences,
                    AnalyticsService,
                    BackgroundService,
                    InteractorFactory,
                    SchedulerProvider,
                    NavigationService);
        }

        public sealed class TheConstructor : CalendarDayViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useTimeService,
                bool useDataSource,
                bool useRxActionFactory,
                bool useUserPreferences,
                bool useAnalyticsService,
                bool useBackgroundService,
                bool useInteractorFactory,
                bool useSchedulerProvider,
                bool useNavigationService)
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new CalendarDayViewModel(
                        default(DateTimeOffset),
                        useTimeService ? TimeService : null,
                        useDataSource ? DataSource : null,
                        useRxActionFactory ? RxActionFactory : null,
                        useUserPreferences ? UserPreferences : null,
                        useAnalyticsService ? AnalyticsService : null,
                        useBackgroundService ? BackgroundService : null,
                        useInteractorFactory ? InteractorFactory : null,
                        useSchedulerProvider ? SchedulerProvider : null,
                        useNavigationService ? NavigationService : null);

                tryingToConstructWithEmptyParameters.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheCalendarItemsProperty : CalendarDayViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ReturnsTheCalendarItemsForToday()
            {
                var now = new DateTimeOffset(2018, 8, 9, 12, 0, 0, TimeSpan.Zero);
                TimeService.CurrentDateTime.Returns(now);

                var items = new List<CalendarItem>
                {
                    new CalendarItem("id", CalendarItemSource.Calendar, now.AddMinutes(30), TimeSpan.FromMinutes(15), "Weekly meeting", CalendarIconKind.Event, "#ff0000"),
                    new CalendarItem("id", CalendarItemSource.TimeEntry, now.AddHours(-3), TimeSpan.FromMinutes(30), "Bug fixes", CalendarIconKind.None, "#00ff00"),
                    new CalendarItem("id", CalendarItemSource.Calendar, now.AddHours(2), TimeSpan.FromMinutes(30), "F**** timesheets", CalendarIconKind.Event, "#ff0000")
                };
                var interactor = Substitute.For<IInteractor<IObservable<IEnumerable<CalendarItem>>>>();
                interactor.Execute().Returns(Observable.Return(items));
                InteractorFactory.GetCalendarItemsForDate(Arg.Any<DateTime>()).Returns(interactor);

                await ViewModel.Initialize();

                TestScheduler.Start();
                ViewModel.CalendarItems[0].Should().BeEquivalentTo(items);
            }

            [Fact, LogIfTooSlow]
            public async Task RefetchesWheneverATimeEntryIsAdded()
            {
                var midnightSubject = new Subject<DateTimeOffset>();
                var createdSubject = new Subject<IThreadSafeTimeEntry>();
                TimeService.MidnightObservable.Returns(midnightSubject);
                await ViewModel.Initialize();
                CalendarInteractor.ClearReceivedCalls();
                TestScheduler.Start();

                createdSubject.OnNext(new MockTimeEntry());

                TestScheduler.Start();
                await CalendarInteractor.Received().Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task RefetchesWheneverATimeEntryIsUpdated()
            {
                var midnightSubject = new Subject<DateTimeOffset>();
                var updatedSubject = new Subject<EntityUpdate<IThreadSafeTimeEntry>>();
                TimeService.MidnightObservable.Returns(midnightSubject);
                await ViewModel.Initialize();
                CalendarInteractor.ClearReceivedCalls();
                TestScheduler.Start();

                updatedSubject.OnNext(new EntityUpdate<IThreadSafeTimeEntry>(0, new MockTimeEntry()));

                TestScheduler.Start();
                await CalendarInteractor.Received().Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task RefetchesWheneverATimeEntryIsDeleted()
            {
                var deletedSubject = new Subject<long>();
                var midnightSubject = new Subject<DateTimeOffset>();
                TimeService.MidnightObservable.Returns(midnightSubject);
                await ViewModel.Initialize();
                CalendarInteractor.ClearReceivedCalls();
                TestScheduler.Start();

                deletedSubject.OnNext(0);

                TestScheduler.Start();
                await CalendarInteractor.Received().Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task RefetchesWheneverTheDayChanges()
            {
                var midnightSubject = new Subject<DateTimeOffset>();
                TimeService.MidnightObservable.Returns(midnightSubject);
                await ViewModel.Initialize();
                CalendarInteractor.ClearReceivedCalls();
                TestScheduler.Start();

                midnightSubject.OnNext(DateTimeOffset.Now);

                TestScheduler.Start();
                await CalendarInteractor.Received().Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task RefetchesWheneverTheSelectedCalendarsChange()
            {
                var calendarSubject = new Subject<List<string>>();
                var midnightSubject = new Subject<DateTimeOffset>();
                TimeService.MidnightObservable.Returns(midnightSubject);
                UserPreferences.EnabledCalendars.Returns(calendarSubject);
                await ViewModel.Initialize();
                CalendarInteractor.ClearReceivedCalls();
                TestScheduler.Start();

                calendarSubject.OnNext(new List<string>());

                TestScheduler.Start();
                await CalendarInteractor.Received().Execute();
            }
        }

        public abstract class TheOnItemTappedAction : CalendarDayViewModelTest
        {
            protected abstract void EnsureEventWasTracked();

            protected abstract CalendarItem CalendarItem { get; }

            [Fact, LogIfTooSlow]
            public async Task TracksTheAppropriateEventToTheAnalyticsService()
            {
                ViewModel.OnItemTapped.Execute(CalendarItem);
                TestScheduler.Start();

                EnsureEventWasTracked();
            }

            public sealed class WhenHandlingTimeEntryItems : TheOnItemTappedAction
            {
                protected override void EnsureEventWasTracked()
                {
                    AnalyticsService.EditViewOpenedFromCalendar.Received().Track();
                }

                protected override CalendarItem CalendarItem { get; } = new CalendarItem(
                    "id",
                    CalendarItemSource.TimeEntry,
                    new DateTimeOffset(2018, 08, 10, 0, 0, 0, TimeSpan.Zero),
                    TimeSpan.FromMinutes(10),
                    "Working on something",
                    CalendarIconKind.None,
                    "#00FF00",
                    TimeEntryId
                );

                [Fact]
                public async Task NavigatesToTheEditTimeEntryViewModelUsingTheTimeEntryId()
                {
                    ViewModel.OnItemTapped.Execute(CalendarItem);
                    TestScheduler.Start();

                    await NavigationService.Received().Navigate<EditTimeEntryViewModel, long[]>(
                        Arg.Is<long[]>(timeEntriesIds => timeEntriesIds.Length == 1 && timeEntriesIds[0] == TimeEntryId), ViewModel.View);
                }
            }

            public sealed class WhenHandlingCalendarItems : CalendarDayViewModelTest
            {
                private CalendarItem CalendarItem { get; } = new CalendarItem(
                    "id",
                    CalendarItemSource.Calendar,
                    new DateTimeOffset(2018, 08, 10, 0, 15, 0, TimeSpan.Zero),
                    TimeSpan.FromMinutes(10),
                    "Meeting with someone",
                    CalendarIconKind.Event
                );

                [Fact, LogIfTooSlow]
                public async Task CreatesATimeEntryUsingTheCalendarItemInfo()
                {
                    ViewModel.OnItemTapped.Execute(CalendarItem);
                    TestScheduler.Start();

                    await InteractorFactory
                        .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(p => p.Description == CalendarItem.Description), TimeEntryStartOrigin.CalendarEvent)
                        .Received()
                        .Execute();
                }

                [Fact, LogIfTooSlow]
                public async Task CreatesATimeEntryInTheDefaultWorkspace()
                {
                    ViewModel.OnItemTapped.Execute(CalendarItem);
                    TestScheduler.Start();

                    await InteractorFactory
                        .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(p => p.WorkspaceId == DefaultWorkspaceId), TimeEntryStartOrigin.CalendarEvent)
                        .Received()
                        .Execute();
                }
            }
        }

        public sealed class TheOnDurationSelectedAction : CalendarDayViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void CreatesATimeEntryWithTheSelectedStartDate()
            {
                var now = DateTimeOffset.UtcNow;
                var duration = TimeSpan.FromMinutes(30);
                var tuple = (now, duration);

                ViewModel.OnDurationSelected.Execute(tuple);
                TestScheduler.Start();

                InteractorFactory
                    .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(p => p.StartTime == now), TimeEntryStartOrigin.CalendarTapAndDrag)
                    .Received()
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesATimeEntryWithTheSelectedDuration()
            {
                var now = DateTimeOffset.UtcNow;
                var duration = TimeSpan.FromMinutes(30);
                var tuple = (now, duration);

                ViewModel.OnDurationSelected.Execute(tuple);
                TestScheduler.Start();

                await InteractorFactory
                    .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(p => p.Duration == duration), TimeEntryStartOrigin.CalendarTapAndDrag)
                    .Received()
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesATimeEntryInTheDefaultWorkspace()
            {
                var now = DateTimeOffset.UtcNow;
                var duration = TimeSpan.FromMinutes(30);
                var tuple = (now, duration);

                ViewModel.OnDurationSelected.Execute(tuple);
                TestScheduler.Start();

                await InteractorFactory
                    .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(p => p.WorkspaceId == DefaultWorkspaceId), TimeEntryStartOrigin.CalendarTapAndDrag)
                    .Received()
                    .Execute();
            }

            [Fact]
            public async Task NavigatesToTheEditTimeEntryViewModelUsingTheTimeEntryId()
            {
                var now = DateTimeOffset.UtcNow;
                var duration = TimeSpan.FromMinutes(30);
                var tuple = (now, duration);

                ViewModel.OnDurationSelected.Execute(tuple);
                TestScheduler.Start();

                await NavigationService.Received().Navigate<EditTimeEntryViewModel, long[]>(
                    Arg.Is<long[]>(timeEntriesIds => timeEntriesIds.Length == 1 && timeEntriesIds[0] == TimeEntryId), ViewModel.View);
            }
        }

        public sealed class TheEditTimeEntryAction : CalendarDayViewModelTest
        {
            private CalendarItem calendarItem = new CalendarItem(
                "id",
                CalendarItemSource.TimeEntry,
                new DateTimeOffset(2018, 8, 20, 10, 0, 0, TimeSpan.Zero),
                new TimeSpan(45),
                "This is a time entry",
                CalendarIconKind.None,
                color: "#ff0000",
                timeEntryId: TimeEntryId,
                calendarId: "abcd-1234-abcd-1234");

            [Fact, LogIfTooSlow]
            public async Task UpdatesATimeEntry()
            {
                ViewModel.OnTimeEntryEdited.Execute(calendarItem);

                await InteractorFactory
                    .UpdateTimeEntry(Arg.Is<DTOs.EditTimeEntryDto>(dto =>
                        dto.Id == TimeEntryId
                        && dto.StartTime == calendarItem.StartTime
                        && dto.StopTime == calendarItem.EndTime))
                    .Received()
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task TracksTheTimeEntryEditedFromCalendarEventWhenDurationChanges()
            {
                var timeEntry = new MockTimeEntry
                {
                    Start = calendarItem.StartTime,
                    Duration = (long)calendarItem.Duration.Value.TotalSeconds + 10
                };
                InteractorFactory.GetTimeEntryById(Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(timeEntry));

                ViewModel.OnTimeEntryEdited.Execute(calendarItem);

                AnalyticsService.TimeEntryChangedFromCalendar.Received().Track(CalendarChangeEvent.Duration);
            }

            [Fact, LogIfTooSlow]
            public async Task TracksTheTimeEntryEditedFromCalendarEventWhenStartTimeChanges()
            {
                var timeEntry = new MockTimeEntry
                {
                    Start = calendarItem.StartTime.Add(TimeSpan.FromHours(1)),
                    Duration = (long)calendarItem.Duration.Value.TotalSeconds
                };
                InteractorFactory.GetTimeEntryById(Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(timeEntry));

                ViewModel.OnTimeEntryEdited.Execute(calendarItem);

                AnalyticsService.TimeEntryChangedFromCalendar.Received().Track(CalendarChangeEvent.StartTime);
            }

            [Fact, LogIfTooSlow]
            public async Task TracksTheTimeEntryEditedFromCalendarEventWhenBothStartTimeAndDurationChange()
            {
                var timeEntry = new MockTimeEntry
                {
                    Start = calendarItem.StartTime.Add(TimeSpan.FromHours(1)),
                    Duration = (long)calendarItem.Duration.Value.TotalSeconds + 10
                };
                InteractorFactory.GetTimeEntryById(Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(timeEntry));

                ViewModel.OnTimeEntryEdited.Execute(calendarItem);

                AnalyticsService.TimeEntryChangedFromCalendar.Received().Track(CalendarChangeEvent.Duration);
                AnalyticsService.TimeEntryChangedFromCalendar.Received().Track(CalendarChangeEvent.StartTime);
            }
        }

        public sealed class TheCalendarEventLongPressedAction : CalendarDayViewModelTest
        {
            private CalendarItem calendarEvent = new CalendarItem(
                "id",
                CalendarItemSource.Calendar,
                new DateTimeOffset(2018, 8, 20, 10, 0, 0, TimeSpan.Zero),
                new TimeSpan(45),
                "This is a calendar event",
                CalendarIconKind.None,
                color: "#ff0000",
                timeEntryId: TimeEntryId,
                calendarId: "abcd-1234-abcd-1234");

            [Fact, LogIfTooSlow]
            public async Task PresentsTwoOptionsToTheUserWhenTheEventStartsInTheFuture()
            {
                TimeService.CurrentDateTime.Returns(calendarEvent.StartTime - TimeSpan.FromHours(1));

                ViewModel.OnCalendarEventLongPressed.Inputs.OnNext(calendarEvent);

                await View.Received().SelectAction(
                    Arg.Any<string>(),
                    Arg.Is<IEnumerable<SelectOption<CalendarItem?>>>(options => options.Count() == 2));
            }

            [Fact, LogIfTooSlow]
            public async Task PresentsThreeOptionsToTheUserWhenTheEventStartsInThePast()
            {
                TimeService.CurrentDateTime.Returns(calendarEvent.StartTime + TimeSpan.FromHours(1));

                ViewModel.OnCalendarEventLongPressed.Inputs.OnNext(calendarEvent);

                await View.Received().SelectAction(
                    Arg.Any<string>(),
                    Arg.Is<IEnumerable<SelectOption<CalendarItem?>>>(options => options.Count() == 3));
            }

            [Fact, LogIfTooSlow]
            public void DoesNotCreateAnyTimeEntryWhenUserSelectsTheCancelOption()
            {
                View.Select<CalendarItem?>(null, null, 0)
                    .ReturnsForAnyArgs(Observable.Return<CalendarItem?>(null));

                ViewModel.OnCalendarEventLongPressed.Inputs.OnNext(calendarEvent);

                InteractorFactory
                    .DidNotReceive()
                    .CreateTimeEntry(Arg.Any<ITimeEntryPrototype>(), TimeEntryStartOrigin.CalendarEvent);
            }

            [Fact, LogIfTooSlow]
            public void AllowsCopyingOfTheCalendarEventIntoATimeEntry()
            {
                selectOptionByOptionText(Resources.CalendarCopyEventToTimeEntry);

                ViewModel.OnCalendarEventLongPressed.Inputs.OnNext(calendarEvent);

                InteractorFactory
                    .Received()
                    .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(
                        te => te.StartTime == calendarEvent.StartTime && te.Duration == calendarEvent.Duration), TimeEntryStartOrigin.CalendarEvent);
            }

            [Fact, LogIfTooSlow]
            public void AllowsStartingTheCalendarEventWithTheStartTimeSetToNow()
            {
                selectOptionByOptionText(Resources.CalendarStartNow);

                ViewModel.OnCalendarEventLongPressed.Inputs.OnNext(calendarEvent);

                InteractorFactory
                    .Received()
                    .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(
                        te => te.StartTime == Now && te.Duration == null), TimeEntryStartOrigin.CalendarEvent);
            }

            [Fact, LogIfTooSlow]
            public void AllowsStartingTheCalendarEventWithTheStartTimeSetToTheCalendarEventStartWhenItStartsInThePast()
            {
                TimeService.CurrentDateTime.Returns(calendarEvent.StartTime + TimeSpan.FromMinutes(37));
                selectOptionByOptionText(Resources.CalendarStartWhenTheEventStarts);

                ViewModel.OnCalendarEventLongPressed.Inputs.OnNext(calendarEvent);

                InteractorFactory
                    .Received()
                    .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(
                        te => te.StartTime == calendarEvent.StartTime && te.Duration == null), TimeEntryStartOrigin.CalendarEvent);
            }

            private void selectOptionByOptionText(string text)
            {
                View.SelectAction<CalendarItem?>(null, null)
                    .ReturnsForAnyArgs(callInfo =>
                    {
                        var copyOption = callInfo.Arg<IEnumerable<SelectOption<CalendarItem?>>>()
                            .Single(option => option.ItemName == text)
                            .Item;
                        return Observable.Return(copyOption);
                    });
            }
        }

    }
}
