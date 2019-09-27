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
        private const int minAllowedPageIndex = -14;
        private const int maxAllowedPageIndex = 0;

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

            ViewModel.CurrentlyShownDateString
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
            var currentDayViewController = viewControllerAtIndex(0);
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
            var referenceTag = referenceViewController.View.Tag;
            if (referenceTag == minAllowedPageIndex)
                return null;

            return viewControllerAtIndex(referenceTag -1);
        }

        public UIViewController GetNextViewController(UIPageViewController pageViewController, UIViewController referenceViewController)
        {
            var referenceTag = referenceViewController.View.Tag;
            if (referenceTag == maxAllowedPageIndex)
                return null;

            return viewControllerAtIndex(referenceTag + 1);
        }

        [Export("pageViewController:willTransitionToViewControllers:")]
        public void WillTransition(UIPageViewController pageViewController, UIViewController[] pendingViewControllers)
        {
            var pendingCalendarDayViewController = pendingViewControllers.FirstOrDefault() as CalendarDayViewController;
            if (pendingCalendarDayViewController == null)
                return;

            var currentCalendarDayViewController = pageViewController.ViewControllers.FirstOrDefault() as CalendarDayViewController;
            if (currentCalendarDayViewController == null) return;

            pendingCalendarDayViewController.SetScrollOffset(currentCalendarDayViewController.ScrollOffset);
        }

        private CalendarDayViewController viewControllerAtIndex(nint index)
        {
            var date = DateTimeOffset.Now.AddDays(index);
            var viewModel = ViewModel.DayViewModelFor(date);
            var viewController = new CalendarDayViewController(viewModel);
            viewController.View.Tag = index;
            return viewController;
        }

        [Export("pageViewController:didFinishAnimating:previousViewControllers:transitionCompleted:")]
        public void DidFinishAnimating(UIPageViewController pageViewController, bool finished, UIViewController[] previousViewControllers, bool completed)
        {
            if (!completed) return;

            var currentIndex = pageViewController.ViewControllers.FirstOrDefault()?.View?.Tag;
            if (currentIndex == null) return;
            ViewModel.CurrentlyVisiblePage.Accept((int)currentIndex.Value);
        }
    }
}
