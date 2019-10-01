using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Reactive.Linq;
using Toggl.Core.UI.ViewModels.Calendar;
using Toggl.Droid.Extensions;
using Toggl.Droid.Extensions.Reactive;
using Toggl.Droid.Presentation;
using Toggl.Shared.Extensions;

namespace Toggl.Droid.Fragments
{
    public partial class CalendarFragment : ReactiveTabFragment<CalendarViewModel>, IScrollableToStart
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.CalendarFragment, container, false);
            InitializeViews(view);
            calendarDayView.AttachMaterialScrollBehaviour(appBarLayout);
            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            var timeService = AndroidDependencyContainer.Instance.TimeService;
            var schedulerProvider = AndroidDependencyContainer.Instance.SchedulerProvider;
            
            var displayMetrics = new DisplayMetrics();
            Activity.WindowManager.DefaultDisplay.GetMetrics(displayMetrics);
            
            timeService
                .CurrentDateTimeObservable
                .DistinctUntilChanged(offset => offset.Day)
                .ObserveOn(schedulerProvider.MainScheduler)
                .Subscribe(configureHeaderDate)
                .DisposedBy(DisposeBag);

            calendarDayView.UpdateItems(ViewModel.CalendarItems);
            
            ViewModel.CalendarItems.CollectionChange
                .Subscribe(_ => calendarDayView.UpdateItems(ViewModel.CalendarItems))
                .DisposedBy(DisposeBag);
            
            ViewModel.TimeTrackedToday
                .Subscribe(headerTimeEntriesDurationTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);
            
            calendarDayView.CalendarItemTappedObservable
                .Subscribe(ViewModel.OnItemTapped.Inputs)
                .DisposedBy(DisposeBag);

            calendarDayView.EmptySpansTouchedObservable
                .Subscribe(ViewModel.CreateTimeEntryAtOffset.Inputs)
                .DisposedBy(DisposeBag);

            calendarDayView.EditCalendarItem
                .Subscribe(ViewModel.OnUpdateTimeEntry.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.ShouldShowOnboarding
                .Subscribe(onboardingVisibilityChanged)
                .DisposedBy(DisposeBag);
        }

        public void ScrollToStart()
        {
            calendarDayView?.ScrollToCurrentHour(true);
        }

        public override void OnResume()
        {
            base.OnResume();
            calendarDayView?.ScrollToCurrentHour();
        }

        private void onboardingVisibilityChanged(bool visible)
        {
            if (visible)
            {
                initializeOnboardingViewIfNeeded();

                appBarLayout.Visibility = ViewStates.Gone;
                onboardingView.Visibility = ViewStates.Visible;
                calendarDayView.Visibility = ViewStates.Gone;
                return;
            }

            appBarLayout.Visibility = ViewStates.Visible;
            calendarDayView.Visibility = ViewStates.Visible;

            if (onboardingView == null)
                return;

            onboardingView.Visibility = ViewStates.Gone;
        }

        private void initializeOnboardingViewIfNeeded()
        {
            if (onboardingView != null)
                return;

            onboardingView = onboardingViewStub.Inflate();
            onboardingTitleView = onboardingView.FindViewById<TextView>(Resource.Id.CalendarOnboardingTitle);
            onboardingMessageView = onboardingView.FindViewById<TextView>(Resource.Id.CalendarOnboardingMessage);
            getStartedButton = onboardingView.FindViewById<Button>(Resource.Id.CalendarOnboardingGetStartedButton);
            skipButton = onboardingView.FindViewById<TextView>(Resource.Id.CalendarOnboardingSkipButton);

            onboardingTitleView.Text = Shared.Resources.CalendarOnboardingTitle;
            onboardingMessageView.Text = Shared.Resources.CalendarOnboardingMessage;
            getStartedButton.Text = Shared.Resources.LinkYourCalendars;
            skipButton.Text = Shared.Resources.Skip;

            getStartedButton.Rx().Tap()
                .Subscribe(ViewModel.GetStarted.Inputs)
                .DisposedBy(DisposeBag);

            skipButton.Rx().Tap()
                .Subscribe(ViewModel.SkipOnboarding.Inputs)
                .DisposedBy(DisposeBag);
        }

        private void configureHeaderDate(DateTimeOffset offset)
        {
            var dayText = offset.ToString(Shared.Resources.CalendarToolbarDateFormat);
            headerDateTextView.Text = dayText;
        }
    }
}
