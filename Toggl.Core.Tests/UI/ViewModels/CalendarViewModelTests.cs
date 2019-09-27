using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Toggl.Core.Analytics;
using Toggl.Core.Calendar;
using Toggl.Core.DataSources;
using Toggl.Core.DTOs;
using Toggl.Core.Interactors;
using Toggl.Core.Models.Interfaces;
using Toggl.Core.Tests.Generators;
using Toggl.Core.Tests.Mocks;
using Toggl.Core.Tests.TestExtensions;
using Toggl.Core.UI.Navigation;
using Toggl.Core.UI.Parameters;
using Toggl.Core.UI.ViewModels;
using Toggl.Core.UI.ViewModels.Calendar;
using Toggl.Core.UI.Views;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Xunit;
using ITimeEntryPrototype = Toggl.Core.Models.ITimeEntryPrototype;
using Notification = System.Reactive.Notification;

namespace Toggl.Core.Tests.UI.ViewModels
{
    public sealed class CalendarViewModelTests
    {
        public abstract class CalendarViewModelTest : BaseViewModelTests<CalendarViewModel>
        {
            protected const long TimeEntryId = 10;
            protected const long DefaultWorkspaceId = 1;

            protected static DateTimeOffset Now { get; } = new DateTimeOffset(2018, 8, 10, 12, 0, 0, TimeSpan.Zero);

            protected IInteractor<IObservable<IEnumerable<CalendarItem>>> CalendarInteractor { get; }

            protected CalendarViewModelTest()
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

            protected override CalendarViewModel CreateViewModel()
                => new CalendarViewModel(
                    DataSource,
                    TimeService,
                    UserPreferences,
                    AnalyticsService,
                    BackgroundService,
                    InteractorFactory,
                    OnboardingStorage,
                    SchedulerProvider,
                    PermissionsChecker,
                    NavigationService,
                    RxActionFactory
                );
        }

        public sealed class TheConstructor : CalendarViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource,
                bool useTimeService,
                bool useUserPreferences,
                bool useAnalyticsService,
                bool useBackgroundService,
                bool useInteractorFactory,
                bool useOnboardingStorage,
                bool useSchedulerProvider,
                bool useNavigationService,
                bool usePermissionsChecker,
                bool useRxActionFactory)
            {
                var dataSource = useDataSource ? DataSource : null;
                var timeService = useTimeService ? TimeService : null;
                var userPreferences = useUserPreferences ? UserPreferences : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var backgroundService = useBackgroundService ? BackgroundService : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var permissionsService = usePermissionsChecker ? PermissionsChecker : null;
                 var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new CalendarViewModel(
                        dataSource,
                        timeService,
                        userPreferences,
                        analyticsService,
                        backgroundService,
                        interactorFactory,
                        onboardingStorage,
                        schedulerProvider,
                        permissionsService,
                        navigationService,
                        rxActionFactory);

                tryingToConstructWithEmptyParameters.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheInitMethod : CalendarViewModelTest
        {
            private const string eventId = "1337";

            private readonly CalendarItem calendarItem = new CalendarItem(
                eventId,
                CalendarItemSource.Calendar,
                new DateTimeOffset(2018, 08, 10, 0, 15, 0, TimeSpan.Zero),
                TimeSpan.FromMinutes(10),
                "Meeting with someone",
                CalendarIconKind.Event
            );

            public TheInitMethod()
            {
                InteractorFactory
                    .GetCalendarItemWithId(Arg.Is(eventId))
                    .Execute()
                    .Returns(Observable.Return(calendarItem));
            }

            [Theory, LogIfTooSlow]
            [InlineData("")]
            [InlineData(" ")]
            [InlineData("  ")]
            public void DoesNothingWithInvalidStrings(string invalidId)
            {
                ViewModel.Init(invalidId);

                InteractorFactory
                    .DidNotReceive()
                    .GetCalendarItemWithId(Arg.Any<string>());
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesATimeEntryUsingTheCalendarItemInfo()
            {
                ViewModel.Init(eventId);

                await InteractorFactory
                    .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(p => p.Description == calendarItem.Description), TimeEntryStartOrigin.CalendarEvent)
                    .Received()
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesATimeEntryInTheDefaultWorkspace()
            {
                ViewModel.Init(eventId);

                await InteractorFactory
                    .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(p => p.WorkspaceId == DefaultWorkspaceId), TimeEntryStartOrigin.CalendarEvent)
                    .Received()
                    .Execute();
            }
        }

        public sealed class TheShouldShowOnboardingProperty : CalendarViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ReturnsTrueIfCalendarOnboardingHasntBeenCompleted()
            {
                OnboardingStorage.CompletedCalendarOnboarding().Returns(false);
                var viewModel = CreateViewModel();
                var observer = TestScheduler.CreateObserver<bool>();

                viewModel.ShouldShowOnboarding.Subscribe(observer);

                TestScheduler.Start();
                observer.SingleEmittedValue().Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsFalseIfCalendarOnboardingHasBeenCompleted()
            {
                OnboardingStorage.CompletedCalendarOnboarding().Returns(true);
                var viewModel = CreateViewModel();
                var observer = TestScheduler.CreateObserver<bool>();

                viewModel.ShouldShowOnboarding.Subscribe(observer);
                TestScheduler.Start();

                observer.SingleEmittedValue().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task EmitsFalseWhenUserGrantsCalendarAccess()
            {
                OnboardingStorage.CompletedCalendarOnboarding().Returns(false);
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.ShouldShowOnboarding.Subscribe(observer);
                View.RequestCalendarAuthorization().Returns(Observable.Return(true));
                NavigationService.Navigate<SelectUserCalendarsViewModel, bool, string[]>(Arg.Any<bool>(), ViewModel.View).Returns(new string[0]);

                ViewModel.GetStarted.Execute();
                TestScheduler.Start();

                observer.Messages.Select(message => message.Value.Value).AssertEqual(true, false);
            }

            [Fact, LogIfTooSlow]
            public async Task EmitsFalseWhenUserWantsToContinueWithoutCalendarAccess()
            {
                OnboardingStorage.CompletedCalendarOnboarding().Returns(false);
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.ShouldShowOnboarding.Subscribe(observer);
                View.RequestCalendarAuthorization().Returns(Observable.Return(false));
                NavigationService.Navigate<CalendarPermissionDeniedViewModel, Unit>(ViewModel.View).Returns(Unit.Default);

                ViewModel.GetStarted.Execute();
                TestScheduler.Start();

                observer.Messages.Select(message => message.Value.Value).AssertEqual(true, false);
            }
        }

        public sealed class TheSettingsAreVisibleObservable : CalendarViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task EmitsWheneverTheShouldShowOnboardingObservablesOmits()
            {
                PermissionsChecker.CalendarPermissionGranted.Returns(Observable.Return(false));
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.SettingsAreVisible.Subscribe(observer);
                PermissionsChecker.CalendarPermissionGranted.Returns(Observable.Return(true));
                ViewModel.GetStarted.Execute();

                TestScheduler.Start();

                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(0, false),
                    ReactiveTest.OnNext(0, true)
                );
            }
        }

        public sealed class TheSelectCalendarsAction : CalendarViewModelTest
        {
            protected override void AdditionalSetup()
            {
                OnboardingStorage
                    .CompletedCalendarOnboarding()
                    .Returns(true);

                PermissionsChecker
                    .CalendarPermissionGranted
                    .Returns(Observable.Return(true));

                NavigationService
                    .Navigate<SelectUserCalendarsViewModel, bool, string[]>(Arg.Any<bool>(), View)
                    .Returns(new string[0]);

                InteractorFactory
                    .GetUserCalendars()
                    .Execute()
                    .Returns(Observable.Return(new UserCalendar().Yield()));

                View
                    .Alert(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                    .Returns(Observable.Return(Unit.Default));

                TestScheduler.AdvanceBy(1);
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheSelectUserCalendarsViewModelWhenThereAreCalendars()
            {
                ViewModel.SelectCalendars.Execute();
                TestScheduler.Start();

                await NavigationService
                    .Received()
                    .Navigate<SelectUserCalendarsViewModel, bool, string[]>(Arg.Any<bool>(), ViewModel.View);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotNavigateToTheSelectUserCalendarsViewModelWhenThereAreNoCalendars()
            {
                InteractorFactory.GetUserCalendars().Execute().Returns(
                    Observable.Return(new UserCalendar[0])
                );

                ViewModel.SelectCalendars.Execute();
                TestScheduler.Start();

                await View.Received()
                    .Alert(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsADialogWhenThereAreNoCalendars()
            {
                InteractorFactory.GetUserCalendars().Execute().Returns(
                    Observable.Return(new UserCalendar[0])
                );

                ViewModel.SelectCalendars.Execute();
                TestScheduler.Start();

                await NavigationService.DidNotReceive()
                    .Navigate<SelectUserCalendarsViewModel, bool, string[]>(Arg.Any<bool>(), ViewModel.View);
            }

            [Property]
            public void SetsTheEnabledCalendarsWhenThereAreCalendars(NonEmptyString[] nonEmptyStrings)
            {
                if (nonEmptyStrings == null) return;

                InteractorFactory.ClearReceivedCalls();
                var viewModel = CreateViewModel();
                viewModel.AttachView(View);
                var calendarIds = nonEmptyStrings.Select(str => str.Get).ToArray();
                NavigationService.Navigate<SelectUserCalendarsViewModel, bool, string[]>(Arg.Any<bool>(), viewModel.View).Returns(calendarIds);
                View.RequestCalendarAuthorization().Returns(Observable.Return(true));
                InteractorFactory.GetUserCalendars().Execute().Returns(
                    Observable.Return(new UserCalendar[] { new UserCalendar() })
                );

                viewModel.SelectCalendars.Execute();
                TestScheduler.Start();

                InteractorFactory.Received().SetEnabledCalendars(calendarIds).Execute();
            }
        }

        public abstract class LinkCalendarsTest : CalendarViewModelTest
        {
            protected abstract UIAction Action { get; }

            [Fact, LogIfTooSlow]
            public async Task RequestsCalendarPermission()
            {
                Action.Execute();
                TestScheduler.Start();

                await View.Received().RequestCalendarAuthorization();
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheCalendarPermissionDeniedViewModelWhenPermissionIsDenied()
            {
                View.RequestCalendarAuthorization().Returns(Observable.Return(false));

                Action.Execute();

                NavigationService.Received().Navigate<CalendarPermissionDeniedViewModel, Unit>(View);
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheSelectUserCalendarsViewModelWhenThereAreCalendars()
            {
                View.RequestCalendarAuthorization().Returns(Observable.Return(true));
                InteractorFactory.GetUserCalendars().Execute().Returns(
                    Observable.Return(new UserCalendar[] { new UserCalendar() })
                );

                Action.Execute();
                TestScheduler.Start();

                await NavigationService
                    .Received()
                    .Navigate<SelectUserCalendarsViewModel, bool, string[]>(Arg.Any<bool>(), ViewModel.View);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotNavigateToTheSelectUserCalendarsViewModelWhenThereAreNoCalendars()
            {
                View.RequestCalendarAuthorization().Returns(Observable.Return(true));
                InteractorFactory.GetUserCalendars().Execute().Returns(
                    Observable.Return(new UserCalendar[0])
                );

                Action.Execute();
                TestScheduler.Start();

                await NavigationService.DidNotReceive().Navigate<SelectUserCalendarsViewModel, bool, string[]>(Arg.Any<bool>(), ViewModel.View);
            }

            [Property]
            public void SetsTheEnabledCalendarsWhenThereAreCalendars(NonEmptyString[] nonEmptyStrings)
            {
                if (nonEmptyStrings == null) return;

                InteractorFactory.ClearReceivedCalls();
                var viewModel = CreateViewModel();
                viewModel.AttachView(View);
                var calendarIds = nonEmptyStrings.Select(str => str.Get).ToArray();
                NavigationService.Navigate<SelectUserCalendarsViewModel, bool, string[]>(Arg.Any<bool>(), viewModel.View).Returns(calendarIds);
                View.RequestCalendarAuthorization().Returns(Observable.Return(true));
                InteractorFactory.GetUserCalendars().Execute().Returns(
                    Observable.Return(new UserCalendar[] { new UserCalendar() })
                );

                Action.Execute(Unit.Default);
                TestScheduler.Start();

                InteractorFactory.Received().SetEnabledCalendars(calendarIds).Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task RequestsNotificationsPermissionIfCalendarPermissionWasGranted()
            {
                View.RequestCalendarAuthorization().Returns(Observable.Return(true));
                NavigationService.Navigate<SelectUserCalendarsViewModel, bool, string[]>(Arg.Any<bool>(), ViewModel.View).Returns(new string[0]);

                Action.Execute(Unit.Default);
                TestScheduler.Start();

                await View.Received().RequestNotificationAuthorization();
            }

            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public async Task SetsTheNotificationPropertyAfterAskingForPermission(bool permissionWasGiven)
            {
                View.RequestCalendarAuthorization().Returns(Observable.Return(true));
                NavigationService.Navigate<SelectUserCalendarsViewModel, bool, string[]>(Arg.Any<bool>(), ViewModel.View).Returns(new string[0]);
                View.RequestNotificationAuthorization().Returns(Observable.Return(permissionWasGiven));

                Action.Execute();
                TestScheduler.Start();

                UserPreferences.Received().SetCalendarNotificationsEnabled(Arg.Is(permissionWasGiven));
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotRequestNotificationsPermissionIfCalendarPermissionWasNotGranted()
            {
                View.RequestCalendarAuthorization().Returns(Observable.Return(false));
                NavigationService.Navigate<SelectUserCalendarsViewModel, bool, string[]>(Arg.Any<bool>(), ViewModel.View).Returns(new string[0]);

                Action.Execute();
                TestScheduler.Start();

                await View.DidNotReceive().RequestNotificationAuthorization();
            }
        }

        public sealed class TheLinkCalendarsAction : LinkCalendarsTest
        {
            protected override UIAction Action => ViewModel.LinkCalendars;
        }

        public sealed class TheHasCalendarsLinkedObservable : CalendarViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task EmitsFalseOnceWhenThereAreNoCalendarsEnabled()
            {
                UserPreferences.EnabledCalendars.Returns(Observable.Return(new List<string>()));
                PermissionsChecker.CalendarPermissionGranted.Returns(Observable.Return(true));
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();
                viewModel.HasCalendarsLinked.Subscribe(observer);

                await viewModel.Initialize();
                TestScheduler.Start();
                viewModel.ViewAppeared();
                TestScheduler.Start();

                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(0, false));
            }

            [Fact, LogIfTooSlow]
            public async Task EmitsTrueWhenThereAreCalendarsEnabled()
            {
                UserPreferences.EnabledCalendars.Returns(Observable.Return(new List<string> { "nice event" }));
                PermissionsChecker.CalendarPermissionGranted.Returns(Observable.Return(true));
                var observer = TestScheduler.CreateObserver<bool>();

                var viewModel = CreateViewModel();

                viewModel.HasCalendarsLinked.Subscribe(observer);

                await viewModel.Initialize();
                TestScheduler.Start();
                viewModel.ViewAppeared();
                TestScheduler.Start();

                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(0, true));
            }

            [Fact, LogIfTooSlow]
            public async Task EmitsFalseWhenCalendarPermissionsWereNotGranted()
            {
                UserPreferences.EnabledCalendars.Returns(Observable.Return(new List<string> { "nice event" }));
                PermissionsChecker.CalendarPermissionGranted.Returns(Observable.Return(false));
                var observer = TestScheduler.CreateObserver<bool>();

                var viewModel = CreateViewModel();

                viewModel.HasCalendarsLinked.Subscribe(observer);

                await viewModel.Initialize();
                TestScheduler.Start();
                viewModel.ViewAppeared();
                TestScheduler.Start();

                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(0, false));
            }

            [Fact, LogIfTooSlow]
            public async Task EmitsFalseWhenCalendarPermissionsWereNotGrantedBeforeAppearedThenTrueIfPermissionWasGrantedAfterAppeared()
            {
                UserPreferences.EnabledCalendars.Returns(Observable.Return(new List<string> { "nice event" }));
                PermissionsChecker.CalendarPermissionGranted.Returns(Observable.Return(false));
                var observer = TestScheduler.CreateObserver<bool>();

                var viewModel = CreateViewModel();

                viewModel.HasCalendarsLinked.Subscribe(observer);

                await viewModel.Initialize();
                TestScheduler.Start();

                PermissionsChecker.CalendarPermissionGranted.Returns(Observable.Return(true));
                TestScheduler.AdvanceTo(100);
                viewModel.ViewAppeared();
                TestScheduler.Start();

                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(0, false),
                    ReactiveTest.OnNext(100, true)
                );
            }

            [Fact, LogIfTooSlow]
            public async Task EmitsFalseWhenThereAreNoCalendarsEnabledThenTrueWhenThereAreCalendarsEnabled()
            {
                var calendars = TestScheduler.CreateHotObservable(
                    new Recorded<Notification<List<string>>>(100, Notification.CreateOnNext(new List<string>())),
                    new Recorded<Notification<List<string>>>(200, Notification.CreateOnNext(new List<string> { "nice event" })));

                UserPreferences.EnabledCalendars.Returns(calendars);
                PermissionsChecker.CalendarPermissionGranted.Returns(Observable.Return(true));
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();
                viewModel.HasCalendarsLinked.Subscribe(observer);

                await viewModel.Initialize();
                TestScheduler.Start();
                viewModel.ViewAppeared();
                TestScheduler.Start();

                UserPreferences.EnabledCalendars.Returns(Observable.Return(new List<string> { "nice event" }));
                TestScheduler.Start();

                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(100, false),
                    ReactiveTest.OnNext(200, true)
                );
            }
        }

        public sealed class TheGetStartedAction : LinkCalendarsTest
        {
            protected override UIAction Action => ViewModel.GetStarted;

            [Fact, LogIfTooSlow]
            public async Task SetsCalendarOnboardingAsCompletedIfUserGrantsAccess()
            {
                View.RequestCalendarAuthorization().Returns(Observable.Return(true));
                NavigationService.Navigate<SelectUserCalendarsViewModel, bool, string[]>(Arg.Any<bool>(), ViewModel.View).Returns(new string[0]);

                Action.Execute(Unit.Default);

                OnboardingStorage.Received().SetCompletedCalendarOnboarding();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsCalendarOnboardingAsCompletedIfUserWantsToContinueWithoutGivingPermission()
            {
                View.RequestCalendarAuthorization().Returns(Observable.Return(false));
                NavigationService.Navigate<CalendarPermissionDeniedViewModel, Unit>(ViewModel.View).Returns(Unit.Default);

                ViewModel.GetStarted.Execute();
                TestScheduler.Start();

                OnboardingStorage.Received().SetCompletedCalendarOnboarding();
            }

            [Fact, LogIfTooSlow]
            public async Task TracksTheCalendarOnbardingStartedEvent()
            {
                View.RequestCalendarAuthorization().Returns(Observable.Return(false));

                ViewModel.GetStarted.Execute();
                TestScheduler.Start();

                AnalyticsService.CalendarOnboardingStarted.Received().Track();
            }
        }

        public sealed class TheSkipOnboardingProperty : CalendarViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task SetsTheOnboardingAsCompleted()
            {
                ViewModel.SkipOnboarding.Execute();
                OnboardingStorage.Received().SetCompletedCalendarOnboarding();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheShouldShowOnboardingPropertyToFalse()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.ShouldShowOnboarding.Subscribe(observer);

                ViewModel.SkipOnboarding.Execute();
                TestScheduler.Start();

                observer.Messages.Select(m => m.Value.Value).Should().BeEquivalentTo(new[] { true, false });
            }
        }

        public sealed class TheCalendarItemsProperty : CalendarViewModelTest
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
                var deletedSubject = new Subject<long>();
                var midnightSubject = new Subject<DateTimeOffset>();
                var createdSubject = new Subject<IThreadSafeTimeEntry>();
                var updatedSubject = new Subject<EntityUpdate<IThreadSafeTimeEntry>>();
                DataSource.TimeEntries.Deleted.Returns(deletedSubject);
                DataSource.TimeEntries.Updated.Returns(updatedSubject);
                DataSource.TimeEntries.Created.Returns(createdSubject);
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
                var deletedSubject = new Subject<long>();
                var midnightSubject = new Subject<DateTimeOffset>();
                var createdSubject = new Subject<IThreadSafeTimeEntry>();
                var updatedSubject = new Subject<EntityUpdate<IThreadSafeTimeEntry>>();
                DataSource.TimeEntries.Deleted.Returns(deletedSubject);
                DataSource.TimeEntries.Updated.Returns(updatedSubject);
                DataSource.TimeEntries.Created.Returns(createdSubject);
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
                var createdSubject = new Subject<IThreadSafeTimeEntry>();
                var updatedSubject = new Subject<EntityUpdate<IThreadSafeTimeEntry>>();
                DataSource.TimeEntries.Deleted.Returns(deletedSubject);
                DataSource.TimeEntries.Updated.Returns(updatedSubject);
                DataSource.TimeEntries.Created.Returns(createdSubject);
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
                var deletedSubject = new Subject<long>();
                var midnightSubject = new Subject<DateTimeOffset>();
                var createdSubject = new Subject<IThreadSafeTimeEntry>();
                var updatedSubject = new Subject<EntityUpdate<IThreadSafeTimeEntry>>();
                DataSource.TimeEntries.Deleted.Returns(deletedSubject);
                DataSource.TimeEntries.Updated.Returns(updatedSubject);
                DataSource.TimeEntries.Created.Returns(createdSubject);
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
                var deletedSubject = new Subject<long>();
                var calendarSubject = new Subject<List<string>>();
                var midnightSubject = new Subject<DateTimeOffset>();
                var createdSubject = new Subject<IThreadSafeTimeEntry>();
                var updatedSubject = new Subject<EntityUpdate<IThreadSafeTimeEntry>>();
                DataSource.TimeEntries.Deleted.Returns(deletedSubject);
                DataSource.TimeEntries.Updated.Returns(updatedSubject);
                DataSource.TimeEntries.Created.Returns(createdSubject);
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

        public abstract class TheOnItemTappedAction : CalendarViewModelTest
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
        }

        public sealed class WhenHandlingCalendarItems : CalendarViewModelTest
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

        public sealed class TheOnDurationSelectedAction : CalendarViewModelTest
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

        public sealed class TheCreateTimeEntryAtOffsetAction : CalendarViewModelTest
        {
            [Fact]
            public async Task NavigatesToTheStartTimeEntryViewModel()
            {
                var offset = DateTimeOffset.UtcNow;
                var duration = TimeSpan.FromMinutes(30);

                ViewModel.CreateTimeEntryAtOffset.Execute(offset);
                TestScheduler.Start();

                await NavigationService.Received().Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(
                    Arg.Is<StartTimeEntryParameters>(param => param.StartTime == offset - duration && param.Duration == duration), ViewModel.View);
            }
        }

        public sealed class TheEditTimeEntryAction : CalendarViewModelTest
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
                ViewModel.OnUpdateTimeEntry.Execute(calendarItem);

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

                ViewModel.OnUpdateTimeEntry.Execute(calendarItem);

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

                ViewModel.OnUpdateTimeEntry.Execute(calendarItem);

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

                ViewModel.OnUpdateTimeEntry.Execute(calendarItem);

                AnalyticsService.TimeEntryChangedFromCalendar.Received().Track(CalendarChangeEvent.Duration);
                AnalyticsService.TimeEntryChangedFromCalendar.Received().Track(CalendarChangeEvent.StartTime);
            }
        }

        public sealed class TheCalendarEventLongPressedAction : CalendarViewModelTest
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

        public sealed class TheTimeTrackedTodayProperty : CalendarViewModelTest
        {
            private readonly ISubject<DurationFormat> durationFormatSubject = new Subject<DurationFormat>();
            private readonly ISubject<TimeSpan> trackedTimeSubject = new Subject<TimeSpan>();

            private static readonly TimeSpan duration = TimeSpan.FromHours(1.5);

            [Theory, LogIfTooSlow]
            [InlineData(DurationFormat.Classic, "00 sec")]
            [InlineData(DurationFormat.Improved, "0:00:00")]
            [InlineData(DurationFormat.Decimal, "00.00 h")]
            public void StartsWithZero(DurationFormat format, string expectedOutput)
            {
                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.TimeTrackedToday.Subscribe(observer);

                durationFormatSubject.OnNext(format);
                TestScheduler.Start();

                observer.Messages.First().Value.Value.Should().Be(expectedOutput);
            }

            [Theory, LogIfTooSlow]
            [InlineData(DurationFormat.Classic, "01:30:00")]
            [InlineData(DurationFormat.Improved, "1:30:00")]
            [InlineData(DurationFormat.Decimal, "01.50 h")]
            public void EmitsCorrectlyFormattedTimeBasedOnUsersPreferences(DurationFormat format, string expectedOutput)
            {
                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.TimeTrackedToday.Subscribe(observer);

                durationFormatSubject.OnNext(format);
                trackedTimeSubject.OnNext(duration);
                TestScheduler.Start();

                observer.Messages.Skip(1).First().Value.Value.Should().Be(expectedOutput);
            }

            protected override void AdditionalSetup()
            {
                var preferencesObservable =
                    durationFormatSubject.Select(format => new MockPreferences { DurationFormat = format } as IThreadSafePreferences);

                DataSource.Preferences.Current
                    .Returns(preferencesObservable);
                InteractorFactory.ObserveTimeTrackedToday().Execute()
                    .Returns(trackedTimeSubject.AsObservable());
            }
        }

        public sealed class TheCurrentDateProperty : CalendarViewModelTest
        {
            private readonly ISubject<DateFormat> dateFormatSubject = new Subject<DateFormat>();
            private readonly ISubject<DateTimeOffset> dateSubject = new Subject<DateTimeOffset>();

            private static readonly DateTimeOffset date = new DateTimeOffset(2019, 01, 19, 23, 50, 00, TimeSpan.FromHours(-1));

            public static IEnumerable<object[]> DatesAndPreferences()
                => new[]
                {
                    new object[] { DateFormat.FromLocalizedDateFormat("YYYY-MM-DD") },
                    new object[] { DateFormat.FromLocalizedDateFormat("DD.MM.YYYY") },
                    new object[] { DateFormat.FromLocalizedDateFormat("DD/MM") }
                };

            [Theory, LogIfTooSlow]
            [MemberData(nameof(DatesAndPreferences))]
            public void EmitsCorrectlyFormattedTimeBasedOnUsersPreferences(DateFormat format)
            {
                var expectedOutput = date.ToLocalTime().ToString(format.Long, CultureInfo.InvariantCulture);
                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.CurrentDate.Subscribe(observer);

                dateFormatSubject.OnNext(format);
                dateSubject.OnNext(date);
                TestScheduler.Start();

                observer.Messages.First().Value.Value.Should().Be(expectedOutput);
            }

            protected override void AdditionalSetup()
            {
                var preferencesObservable =
                    dateFormatSubject.Select(format => new MockPreferences { DateFormat = format } as IThreadSafePreferences);

                DataSource.Preferences.Current
                    .Returns(preferencesObservable);
                TimeService.CurrentDateTimeObservable
                    .Returns(dateSubject.AsObservable());
            }
        }
    }
}
