using FluentAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Core.Models.Interfaces;
using Toggl.Core.Tests.Generators;
using Toggl.Core.Tests.TestExtensions;
using Toggl.Core.UI.Navigation;
using Toggl.Core.UI.ViewModels;
using Toggl.Core.UI.ViewModels.Calendar;
using Toggl.Shared;
using Xunit;

namespace Toggl.Core.Tests.UI.ViewModels
{
    public sealed class CalendarViewModelTests
    {
        public abstract class CalendarViewModelTest : BaseViewModelTests<CalendarViewModel>
        {
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

        public sealed class TheCurrentlyShownDateStringObservable : CalendarViewModelTest
        {
            private static readonly DateTimeOffset now = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero).ToLocalTime().Date;

            public TheCurrentlyShownDateStringObservable()
            {
                TimeService.CurrentDateTime.Returns(now);
            }

            [Fact, LogIfTooSlow]
            public async Task StartsWithTheCurrentDate()
            {
                var preferences = Substitute.For<IThreadSafePreferences>();
                preferences.DateFormat.Returns(DateFormat.ValidDateFormats[0]);
                DataSource.Preferences.Current.Returns(Observable.Return(preferences));
                var expectedResult = "01/02/2020";
                var observer = TestScheduler.CreateObserver<string>();
                var viewModel = CreateViewModel();
                viewModel.CurrentlyShownDateString.Subscribe(observer);

                TestScheduler.Start();

                observer.Values().Should().BeEquivalentTo(new[] { expectedResult });
            }

            [Theory, LogIfTooSlow]
            [InlineData(-1, "01/01/2020")]
            [InlineData(-2, "12/31/2019")]
            [InlineData(-7, "12/26/2019")]
            [InlineData(-14, "12/19/2019")]
            public void EmitsNewDateWhenCurrentlyVisiblePageChanges(int pageIndex, string expectedDate)
            {
                var preferences = Substitute.For<IThreadSafePreferences>();
                preferences.DateFormat.Returns(DateFormat.ValidDateFormats[0]);
                DataSource.Preferences.Current.Returns(Observable.Return(preferences));
                var expectedResults = new[]
                {
                    "01/02/2020",
                    expectedDate
                };
                var observer = TestScheduler.CreateObserver<string>();
                var viewModel = CreateViewModel();
                viewModel.CurrentlyShownDateString.Subscribe(observer);

                TestScheduler.Start();
                viewModel.CurrentlyVisiblePage.Accept(pageIndex);
                TestScheduler.Start();

                observer.Values().Should().BeEquivalentTo(expectedResults);
            }

            [Theory, LogIfTooSlow]
            [MemberData(nameof(DateFormatTestData))]
            public void EmitsNewDateWhenDateFormatChanges(
                DateFormat initialDateFormat, DateFormat updatedDateFormat, string[] expectedResults)
            {
                IThreadSafePreferences preferencesWithDateFormat(DateFormat dateFormat)
                {
                    var preferences = Substitute.For<IThreadSafePreferences>();
                    preferences.DateFormat.Returns(dateFormat);
                    return preferences;
                }
                var initialPreferences = preferencesWithDateFormat(initialDateFormat);
                var updatedPreferences = preferencesWithDateFormat(updatedDateFormat);
                var preferencesObservable = TestScheduler.CreateColdObservable(
                    OnNext(0, initialPreferences),
                    OnNext(10, updatedPreferences)
                );
                DataSource.Preferences.Current.Returns(preferencesObservable);
                var observer = TestScheduler.CreateObserver<string>();
                var viewModel = CreateViewModel();
                viewModel.CurrentlyShownDateString.Subscribe(observer);

                TestScheduler.AdvanceBy(100);

                observer.Values().Should().BeEquivalentTo(expectedResults);
            }

            public static IEnumerable<object[]> DateFormatTestData
            {
                get
                {
                    var initialDateFormat = DateFormat.ValidDateFormats[0];
                    var nowFormattedToInitialFormat = "01/02/2020";
                    return new[]
                    {
                        new object[] { initialDateFormat, DateFormat.ValidDateFormats[1], new[] { nowFormattedToInitialFormat, "02-01-2020" } },
                        new object[] { initialDateFormat, DateFormat.ValidDateFormats[2], new[] { nowFormattedToInitialFormat, "01-02-2020" } },
                        new object[] { initialDateFormat, DateFormat.ValidDateFormats[3], new[] { nowFormattedToInitialFormat, "2020-01-02" } },
                        new object[] { initialDateFormat, DateFormat.ValidDateFormats[4], new[] { nowFormattedToInitialFormat, "02/01/2020" } },
                        new object[] { initialDateFormat, DateFormat.ValidDateFormats[5], new[] { nowFormattedToInitialFormat, "02.01.2020" } }
                    };
                }
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
    }
}
