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
                    RxActionFactory,
                    UserPreferences,
                    AnalyticsService,
                    BackgroundService,
                    InteractorFactory,
                    SchedulerProvider,
                    NavigationService
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
                bool useSchedulerProvider,
                bool useNavigationService,
                bool useRxActionFactory)
            {
                var dataSource = useDataSource ? DataSource : null;
                var timeService = useTimeService ? TimeService : null;
                var userPreferences = useUserPreferences ? UserPreferences : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var backgroundService = useBackgroundService ? BackgroundService : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var navigationService = useNavigationService ? NavigationService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new CalendarViewModel(
                        dataSource,
                        timeService,
                        rxActionFactory,
                        userPreferences,
                        analyticsService,
                        backgroundService,
                        interactorFactory,
                        schedulerProvider,
                        navigationService);

                tryingToConstructWithEmptyParameters.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheOpenSettingsAction : CalendarViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void NavigatesToTheSettingsViewModel()
            {
                ViewModel.OpenSettings.Execute();

                NavigationService.Received().Navigate<SettingsViewModel>(View);
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


//        public sealed class TheCurrentDateProperty : CalendarViewModelTest
//        {
//            private readonly ISubject<DateFormat> dateFormatSubject = new Subject<DateFormat>();
//            private readonly ISubject<DateTimeOffset> dateSubject = new Subject<DateTimeOffset>();
//
//            private static readonly DateTimeOffset date = new DateTimeOffset(2019, 01, 19, 23, 50, 00, TimeSpan.FromHours(-1));
//
//            public static IEnumerable<object[]> DatesAndPreferences()
//                => new[]
//                {
//                    new object[] { DateFormat.FromLocalizedDateFormat("YYYY-MM-DD") },
//                    new object[] { DateFormat.FromLocalizedDateFormat("DD.MM.YYYY") },
//                    new object[] { DateFormat.FromLocalizedDateFormat("DD/MM") }
//                };
//
//            [Theory, LogIfTooSlow]
//            [MemberData(nameof(DatesAndPreferences))]
//            public void EmitsCorrectlyFormattedTimeBasedOnUsersPreferences(DateFormat format)
//            {
//                var expectedOutput = date.ToLocalTime().ToString(format.Long, CultureInfo.InvariantCulture);
//                var observer = TestScheduler.CreateObserver<string>();
//                ViewModel.CurrentDate.Subscribe(observer);
//
//                dateFormatSubject.OnNext(format);
//                dateSubject.OnNext(date);
//                TestScheduler.Start();
//
//                observer.Messages.First().Value.Value.Should().Be(expectedOutput);
//            }
//
//            protected override void AdditionalSetup()
//            {
//                var preferencesObservable =
//                    dateFormatSubject.Select(format => new MockPreferences { DateFormat = format } as IThreadSafePreferences);
//
//                DataSource.Preferences.Current
//                    .Returns(preferencesObservable);
//                TimeService.CurrentDateTimeObservable
//                    .Returns(dateSubject.AsObservable());
//            }
//        }
    }
}
