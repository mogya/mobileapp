using System;
using System.Linq;
using System.Reactive.Linq;
using Foundation;
using Toggl.Core.UI.Transformations;
using Toggl.Core.UI.ViewModels.Calendar;
using Toggl.iOS.Extensions;
using Toggl.iOS.Extensions.Reactive;
using Toggl.iOS.Presentation;
using Toggl.Shared.Extensions;
using UIKit;

namespace Toggl.iOS.ViewControllers.Calendar
{
    public sealed partial class CalendarViewController : ReactiveViewController<NewCalendarViewModel>, IUIPageViewControllerDataSource, IUIPageViewControllerDelegate
    {
        private DateTimeOffset? dateAboutToBeShown;
        private UIPageViewController pageViewController;

        public CalendarViewController(NewCalendarViewModel calendarViewModel)
            : base(calendarViewModel, nameof(CalendarViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            SettingsButton.Rx()
                .BindAction(ViewModel.SelectCalendars)
                .DisposedBy(DisposeBag);

            ViewModel.CurrentlyShownDate
                .Subscribe(SelectedDateLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            DailyTrackedTimeLabel.Font = DailyTrackedTimeLabel.Font.GetMonospacedDigitFont();

            pageViewController = new UIPageViewController(UIPageViewControllerTransitionStyle.Scroll, UIPageViewControllerNavigationOrientation.Horizontal);
            pageViewController.DataSource = this;
            pageViewController.Delegate = this;
            pageViewController.View.Frame = DayViewContainer.Bounds;
            DayViewContainer.AddSubview(pageViewController.View);
            pageViewController.DidMoveToParentViewController(this);

            var today = DateTimeOffset.Now;
            var currentDayViewController = viewControllerForDate(today);
            var viewControllers = new[]
            {
                currentDayViewController
            };
            pageViewController.SetViewControllers(viewControllers, UIPageViewControllerNavigationDirection.Forward, false, null);
        }

        private CalendarDayViewController viewControllerForDate(DateTimeOffset date)
        {
            var vm = ViewModel.DayViewModelFor(date);
            return new CalendarDayViewController(vm);
        }

        public UIViewController GetPreviousViewController(UIPageViewController pageViewController, UIViewController referenceViewController)
        {
            var referenceDate = ((CalendarDayViewController)referenceViewController).ViewModel.Date;
            var previousDay = referenceDate.AddDays(-1);
            var viewModel = ViewModel.DayViewModelFor(previousDay);
            return new CalendarDayViewController(viewModel);
        }

        public UIViewController GetNextViewController(UIPageViewController pageViewController, UIViewController referenceViewController)
        {
            var referenceDate = ((CalendarDayViewController)referenceViewController).ViewModel.Date;
            var previousDay = referenceDate.AddDays(1);
            var viewModel = ViewModel.DayViewModelFor(previousDay);
            return new CalendarDayViewController(viewModel);
        }

        [Export("pageViewController:willTransitionToViewControllers:")]
        public void WillTransition(UIPageViewController pageViewController, UIViewController[] pendingViewControllers)
        {
            var pendingCalendarDayViewController = pendingViewControllers.FirstOrDefault() as CalendarDayViewController;
            if (pendingCalendarDayViewController == null)
                dateAboutToBeShown = null;

            dateAboutToBeShown = pendingCalendarDayViewController.ViewModel.Date;

            var currentCalendarDayViewController = pageViewController.ViewControllers[0] as CalendarDayViewController;
            if (currentCalendarDayViewController == null) return;

            pendingCalendarDayViewController.SetScrollOffset(currentCalendarDayViewController.ScrollOffset);
        }

        [Export("pageViewController:didFinishAnimating:previousViewControllers:transitionCompleted:")]
        public void DidFinishAnimating(UIPageViewController pageViewController, bool finished, UIViewController[] previousViewControllers, bool completed)
        {
            if (!completed) return;
            if (dateAboutToBeShown == null) return;

            ViewModel.UpdateCurrentlyShownDate.Execute(dateAboutToBeShown.Value);
        }
    }
}
