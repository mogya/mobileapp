using System.Reactive.Linq;
using CoreGraphics;
using Toggl.Core;
using Toggl.Core.Calendar;
using Toggl.Core.UI.ViewModels.Calendar;
using Toggl.iOS.Presentation;
using Toggl.iOS.Views.Calendar;
using Toggl.iOS.ViewSources;
using Toggl.Shared.Extensions;
using UIKit;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Toggl.Core.Analytics;
using Toggl.Core.UI.Helper;
using Toggl.Core.UI.ViewModels.Calendar.ContextualMenu;
using Toggl.iOS.Extensions;
using Toggl.iOS.Extensions.Reactive;

namespace Toggl.iOS.ViewControllers
{
    public sealed partial class CalendarDayViewController : ReactiveViewController<CalendarDayViewModel>, IScrollableToTop
    {
        private const double minimumOffsetOfCurrentTimeIndicatorFromScreenEdge = 0.2;
        private const double middleOfTheDay = 12;
        private const int collectionViewHorizontalInset = 20;

        private readonly ITimeService timeService;

        private bool contextualMenuInitialised;

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

            ContextualMenu.Layer.CornerRadius = 8;
            ContextualMenu.Layer.ShadowColor = UIColor.Black.CGColor;
            ContextualMenu.Layer.ShadowOpacity = 0.1f;
            ContextualMenu.Layer.ShadowOffset = new CGSize(0, -2);

            ContextualMenuBottonConstraint.Constant = -ContextualMenu.Frame.Height;

            ContextualMenuFadeView.FadeLeft = true;
            ContextualMenuFadeView.FadeRight = true;

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
            CalendarCollectionView.ContentInset = new UIEdgeInsets(20, 0, 20, 0);

            dataSource.ItemTapped
                .Select(item => (CalendarItem?)item)
                .Subscribe(ViewModel.ContextualMenuViewModel.OnCalendarItemUpdated.Inputs)
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

            //Contextual menu
            ViewModel.ContextualMenuViewModel.CurrentMenu
                .Select(menu => menu.Actions)
                .Subscribe(replaceContextualMenuActions)
                .DisposedBy(DisposeBag);

            ViewModel.ContextualMenuViewModel.MenuVisible
                .Where(isVisible => isVisible)
                .Subscribe(_ => showContextualMenu())
                .DisposedBy(DisposeBag);

            ViewModel.ContextualMenuViewModel.MenuVisible
                .Where(isVisible => !isVisible)
                .Subscribe(_ => dismissContextualMenu())
                .DisposedBy(DisposeBag);

            ContextualMenuCloseButton.Rx().Tap()
                .Subscribe(_ => ViewModel.ContextualMenuViewModel.OnCalendarItemUpdated.Execute(null))
                .DisposedBy(DisposeBag);

            ViewModel.ContextualMenuViewModel.TimeEntryPeriod
                .Subscribe(ContextualMenuTimeEntryPeriodLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.ContextualMenuViewModel.TimeEntryInfo
                .Select(timeEntryInfo => timeEntryInfo.ToAttributedString(ContextualMenuTimeEntryDescriptionProjectTaskClientLabel.Font.CapHeight))
                .Subscribe(ContextualMenuTimeEntryDescriptionProjectTaskClientLabel.Rx().AttributedText())
                .DisposedBy(DisposeBag);

            CalendarCollectionView.LayoutIfNeeded();
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            if (contextualMenuInitialised) return;
            contextualMenuInitialised = true;
            ContextualMenuBottonConstraint.Constant = -ContextualMenu.Frame.Height;
            View.LayoutIfNeeded();
        }

        private void replaceContextualMenuActions(IImmutableList<CalendarMenuAction> actions)
        {
            if (actions == null || actions.Count == 0) return;

            ContextualMenuStackView.ArrangedSubviews.ForEach(view => view.RemoveFromSuperview());

            actions.Select(action => new CalendarContextualMenuActionView(action)
                {
                    TranslatesAutoresizingMaskIntoConstraints = false
                })
                .Do(ContextualMenuStackView.AddArrangedSubview);
        }

        private void showContextualMenu()
        {
            if (!contextualMenuInitialised) return;

            View.LayoutIfNeeded();
            ContextualMenuBottonConstraint.Constant = 0;
            AnimationExtensions.Animate(Animation.Timings.EnterTiming, Animation.Curves.EaseOut, () => View.LayoutIfNeeded());
        }

        private void dismissContextualMenu()
        {
            if (!contextualMenuInitialised) return;

            ContextualMenuBottonConstraint.Constant = -ContextualMenu.Frame.Height;
            AnimationExtensions.Animate(Animation.Timings.EnterTiming, Animation.Curves.EaseOut, () => View.LayoutIfNeeded());
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            updateContentInsetForIpad();
            layout.InvalidateCurrentTimeLayout();
        }

        public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator)
        {
            base.ViewWillTransitionToSize(toSize, coordinator);
            updateContentInsetForIpad();
        }

        private void updateContentInsetForIpad()
        {
            if (TraitCollection.UserInterfaceIdiom != UIUserInterfaceIdiom.Pad) return;

            var deviceOrientation = UIDevice.CurrentDevice.Orientation;
            if (deviceOrientation == UIDeviceOrientation.LandscapeLeft
                || deviceOrientation == UIDeviceOrientation.LandscapeRight)
            {
                //In landscape mode the collection view content should be the same width as in portrait mode
                var screenWidthInPortraitMode = UIScreen.MainScreen.Bounds.Height;
                var collectionViewWidthInPortraitMode = screenWidthInPortraitMode - 2 * collectionViewHorizontalInset;
                var horizontalContentInset = (UIScreen.MainScreen.Bounds.Width - collectionViewWidthInPortraitMode) / 2;
                CalendarCollectionView.ContentInset = new UIEdgeInsets(0, horizontalContentInset, 0, horizontalContentInset);
            }
            else
            {
                CalendarCollectionView.ContentInset = new UIEdgeInsets(0, collectionViewHorizontalInset, 0, collectionViewHorizontalInset);
            }
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
