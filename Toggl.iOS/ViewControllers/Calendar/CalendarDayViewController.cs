using System;
using CoreGraphics;
using Toggl.Core;
using Toggl.Core.UI.ViewModels.Calendar;
using Toggl.iOS.Presentation;
using Toggl.iOS.Views.Calendar;
using Toggl.iOS.ViewSources;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using UIKit;

namespace Toggl.iOS.ViewControllers
{
    public sealed partial class CalendarDayViewController : ReactiveViewController<CalendarDayViewModel>, IScrollableToTop
    {
        private const double minimumOffsetOfCurrentTimeIndicatorFromScreenEdge = 0.2;
        private const double middleOfTheDay = 12;
        private const float collectionViewDefaultInset = 20;
        private const float maxWidth = 834;

        private readonly ITimeService timeService;

        private CalendarCollectionViewLayout layout;
        private CalendarCollectionViewSource dataSource;
        private CalendarCollectionViewEditItemHelper editItemHelper;
        private CalendarCollectionViewCreateFromSpanHelper createFromSpanHelper;
        private CalendarCollectionViewZoomHelper zoomHelper;

        public float ScrollOffset => (float)CalendarCollectionView.ContentOffset.Y;

        public CalendarDayViewController(CalendarDayViewModel viewModel)
            : base(viewModel, nameof(CalendarDayViewController))
        {
            timeService = IosDependencyContainer.Instance.TimeService;
        }

        public void SetScrollOffset(float scrollOffset)
        {
            CalendarCollectionView?.SetContentOffset(new CGPoint(0, scrollOffset), false);
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            TitleLabel.Text = Resources.Welcome;
            DescriptionLabel.Text = Resources.CalendarFeatureDescription;
            GetStartedButton.SetTitle(Resources.GetStarted, UIControlState.Normal);

            dataSource = new CalendarCollectionViewSource(
                timeService,
                CalendarCollectionView,
                ViewModel.TimeOfDayFormat,
                ViewModel.CalendarItems);

            layout = new CalendarCollectionViewLayout(ViewModel.Date.ToLocalTime().Date, timeService, dataSource);

            editItemHelper = new CalendarCollectionViewEditItemHelper(CalendarCollectionView, timeService, dataSource, layout);
            createFromSpanHelper = new CalendarCollectionViewCreateFromSpanHelper(CalendarCollectionView, dataSource, layout);
            zoomHelper = new CalendarCollectionViewZoomHelper(CalendarCollectionView, layout);

            CalendarCollectionView.SetCollectionViewLayout(layout, false);
            CalendarCollectionView.Delegate = dataSource;
            CalendarCollectionView.DataSource = dataSource;

            dataSource.ItemTapped
                .Subscribe(ViewModel.OnItemTapped.Inputs)
                .DisposedBy(DisposeBag);

            editItemHelper.EditCalendarItem
                .Subscribe(ViewModel.OnTimeEntryEdited.Inputs)
                .DisposedBy(DisposeBag);

            editItemHelper.LongPressCalendarEvent
                .Subscribe(ViewModel.OnCalendarEventLongPressed.Inputs)
                .DisposedBy(DisposeBag);

            createFromSpanHelper.CreateFromSpan
                .Subscribe(ViewModel.OnDurationSelected.Inputs)
                .DisposedBy(DisposeBag);

            CalendarCollectionView.LayoutIfNeeded();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            updateContentInset();
            layout.InvalidateCurrentTimeLayout();
        }

        public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator)
        {
            base.ViewWillTransitionToSize(toSize, coordinator);

            updateContentInset();
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            updateContentInset();
        }

        private void updateContentInset()
        {
            if (CalendarCollectionView.Frame.Width <= maxWidth)
            {
                CalendarCollectionView.ContentInset = new UIEdgeInsets(collectionViewDefaultInset, collectionViewDefaultInset, collectionViewDefaultInset * 2, collectionViewDefaultInset);
                return;
            }

            var padding = (CalendarCollectionView.Frame.Width - maxWidth) / 2;
            CalendarCollectionView.ContentInset = new UIEdgeInsets(collectionViewDefaultInset, padding, collectionViewDefaultInset * 2, padding);
        }

        public void ScrollToTop()
        {
            CalendarCollectionView?.SetContentOffset(CGPoint.Empty, true);
        }

        public void SetGoodScrollPoint()
        {
            var frameHeight =
                CalendarCollectionView.Frame.Height
                - CalendarCollectionView.ContentInset.Top
                - CalendarCollectionView.ContentInset.Bottom;
            var hoursOnScreen = frameHeight / (CalendarCollectionView.ContentSize.Height / 24);
            var centeredHour = calculateCenteredHour(timeService.CurrentDateTime.ToLocalTime().TimeOfDay.TotalHours, hoursOnScreen);

            var offsetY = (centeredHour / 24) * CalendarCollectionView.ContentSize.Height - (frameHeight / 2);
            var scrollPointY = offsetY.Clamp(0, CalendarCollectionView.ContentSize.Height - frameHeight);
            var offset = new CGPoint(0, scrollPointY);
            CalendarCollectionView.SetContentOffset(offset, false);
        }

        private static double calculateCenteredHour(double currentHour, double hoursOnScreen)
        {
            var hoursPerHalfOfScreen = hoursOnScreen / 2;
            var minimumOffset = hoursOnScreen * minimumOffsetOfCurrentTimeIndicatorFromScreenEdge;

            var center = (currentHour + middleOfTheDay) / 2;

            if (currentHour < center - hoursPerHalfOfScreen + minimumOffset)
            {
                // the current time indicator would be too close to the top edge of the screen
                return currentHour - minimumOffset + hoursPerHalfOfScreen;
            }

            if (currentHour > center + hoursPerHalfOfScreen - minimumOffset)
            {
                // the current time indicator would be too close to the bottom edge of the screen
                return currentHour + minimumOffset - hoursPerHalfOfScreen;
            }

            return center;
        }
    }
}
