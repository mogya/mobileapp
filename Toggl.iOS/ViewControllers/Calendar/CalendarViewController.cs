using System;
using Toggl.Core.UI.ViewModels.Calendar;
using Toggl.iOS.Presentation;
using UIKit;

namespace Toggl.iOS.ViewControllers.Calendar
{
    public sealed class RandomColorViewController : UIViewController
    {
        private static bool x;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.BackgroundColor = x
                ? UIColor.Blue
                : UIColor.Orange;
            x = !x;
        }
    }

    public sealed partial class CalendarViewController : ReactiveViewController<CalendarViewModel>, IUIPageViewControllerDataSource
    {
        public CalendarViewController(CalendarViewModel calendarViewModel)
            : base(calendarViewModel, nameof(CalendarViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var pageController = new UIPageViewController(UIPageViewControllerTransitionStyle.Scroll, UIPageViewControllerNavigationOrientation.Horizontal);
            pageController.DataSource = this;
            pageController.View.BackgroundColor = UIColor.Black;
            pageController.View.Frame = DayViewContainer.Bounds;
            DayViewContainer.AddSubview(pageController.View);
            pageController.DidMoveToParentViewController(this);

            var calendarViewController = new CalendarDayViewController(ViewModel);
            pageController.SetViewControllers(new[] { calendarViewController }, UIPageViewControllerNavigationDirection.Forward, false, null);
        }

        public UIViewController GetPreviousViewController(UIPageViewController pageViewController,
            UIViewController referenceViewController)
        {
            return new CalendarDayViewController(ViewModel);
        }

        public UIViewController GetNextViewController(UIPageViewController pageViewController,
            UIViewController referenceViewController)
        {
            return new CalendarDayViewController(ViewModel);
        }
    }
}
