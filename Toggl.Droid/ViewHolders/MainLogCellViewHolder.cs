using Android.Graphics;
using Android.Runtime;


using Android.Views;
using Android.Widget;
using System;
using System.Reactive.Subjects;
using AndroidX.ConstraintLayout.Widget;
using AndroidX.Core.Content;
using Toggl.Core.Analytics;
using Toggl.Core.UI.ViewModels.TimeEntriesLog;
using Toggl.Droid.Extensions;
using Toggl.Droid.ViewHelpers;
using static Toggl.Droid.Resource.Id;
using GroupingColor = Toggl.Core.UI.Helper.Colors.TimeEntriesLog.Grouping;

namespace Toggl.Droid.ViewHolders
{
    public class MainLogCellViewHolder : BaseRecyclerViewHolder<TimeEntryViewData>
    {
        public enum AnimationSide
        {
            Left,
            Right
        }

        public MainLogCellViewHolder(View itemView)
            : base(itemView)
        {
        }

        public MainLogCellViewHolder(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        private TextView timeEntriesLogCellDescription;
        private TextView addDescriptionLabel;
        private TextView timeEntriesLogCellProjectLabel;
        private TextView timeEntriesLogCellDuration;
        private ImageView timeEntriesLogCellProjectArchivedIcon;
        private View groupItemBackground;
        private View timeEntriesLogCellContinueImage;
        private View errorImageView;
        private View errorNeedsSync;
        private View timeEntriesLogCellContinueButton;
        private TextView mainLogBackgroundContinue;
        private TextView mainLogBackgroundDelete;
        private View billableIcon;
        private View hasTagsIcon;
        private View durationPadding;
        private View durationFadeGradient;
        private TextView groupCountTextView;
        private View groupExpansionButton;

        public bool CanSync => Item.ViewModel.CanContinue;

        public View MainLogContentView { get; private set; }
        public Subject<(LogItemViewModel, ContinueTimeEntryMode)> ContinueButtonTappedSubject { get; set; }
        public Subject<GroupId> ToggleGroupExpansionSubject { get; set; }

        private GroupId groupId;

        private Color whiteColor;
        private Color grayColor;

        protected override void InitializeViews()
        {
            whiteColor = new Color(ContextCompat.GetColor(ItemView.Context, Resource.Color.mainLogCellPaddingWhite));
            grayColor = new Color(ContextCompat.GetColor(ItemView.Context, Resource.Color.mainLogCellPaddingLightGray));

            groupItemBackground = ItemView.FindViewById<View>(MainLogGroupBackground);
            groupCountTextView = ItemView.FindViewById<TextView>(TimeEntriesLogCellGroupCount);

            timeEntriesLogCellDescription = ItemView.FindViewById<TextView>(TimeEntriesLogCellDescription);
            addDescriptionLabel = ItemView.FindViewById<TextView>(AddDescriptionLabel);
            timeEntriesLogCellProjectLabel = ItemView.FindViewById<TextView>(TimeEntriesLogCellProjectLabel);
            timeEntriesLogCellDuration = ItemView.FindViewById<TextView>(TimeEntriesLogCellDuration);
            timeEntriesLogCellProjectArchivedIcon = ItemView.FindViewById<ImageView>(TimeEntriesLogCellProjectArchivedIcon);
            timeEntriesLogCellContinueImage = ItemView.FindViewById(TimeEntriesLogCellContinueImage);
            errorImageView = ItemView.FindViewById(ErrorImageView);
            errorNeedsSync = ItemView.FindViewById(ErrorNeedsSync);
            timeEntriesLogCellContinueButton = ItemView.FindViewById(TimeEntriesLogCellContinueButton);
            mainLogBackgroundContinue = ItemView.FindViewById<TextView>(MainLogBackgroundContinue);
            mainLogBackgroundDelete = ItemView.FindViewById<TextView>(MainLogBackgroundDelete);
            billableIcon = ItemView.FindViewById(TimeEntriesLogCellBillable);
            hasTagsIcon = ItemView.FindViewById(TimeEntriesLogCellTags);
            durationPadding = ItemView.FindViewById(TimeEntriesLogCellDurationPaddingArea);
            durationFadeGradient = ItemView.FindViewById(TimeEntriesLogCellDurationGradient);
            MainLogContentView = ItemView.FindViewById(Resource.Id.MainLogContentView);

            groupExpansionButton = ItemView.FindViewById(TimeEntriesLogCellToggleExpansionButton);
            timeEntriesLogCellContinueButton.Click += onContinueClick;
            groupExpansionButton.Click += onExpansionClick;

            mainLogBackgroundContinue.Text = Shared.Resources.Continue;
            mainLogBackgroundDelete.Text = Shared.Resources.Delete;
            addDescriptionLabel.Text = Shared.Resources.AddDescription;
        }

        private void onExpansionClick(object sender, EventArgs e)
        {
            ToggleGroupExpansionSubject.OnNext(groupId);
        }

        public void ShowSwipeToContinueBackground()
        {
            mainLogBackgroundContinue.Visibility = ViewStates.Visible;
            mainLogBackgroundDelete.Visibility = ViewStates.Invisible;
        }

        public void ShowSwipeToDeleteBackground()
        {
            mainLogBackgroundContinue.Visibility = ViewStates.Invisible;
            mainLogBackgroundDelete.Visibility = ViewStates.Visible;
        }

        public void HideSwipeBackgrounds()
        {
            mainLogBackgroundContinue.Visibility = ViewStates.Invisible;
            mainLogBackgroundDelete.Visibility = ViewStates.Invisible;
        }

        private void onContinueClick(object sender, EventArgs e)
        {
            var continueMode = Item.ViewModel.IsTimeEntryGroupHeader
                ? ContinueTimeEntryMode.TimeEntriesGroupContinueButton
                : ContinueTimeEntryMode.SingleTimeEntryContinueButton;

            ContinueButtonTappedSubject?.OnNext((Item.ViewModel, continueMode));
        }

        private ConstraintLayout.LayoutParams getDurationPaddingWidthDependentOnIcons()
        {
            var whitePaddingWidth =
                72
                + (Item.ViewModel.IsBillable ? 22 : 0)
                + (Item.ViewModel.HasTags ? 22 : 0);

            var layoutParameters = (ConstraintLayout.LayoutParams)durationPadding.LayoutParameters;
            layoutParameters.Width = whitePaddingWidth.DpToPixels(ItemView.Context);
            return layoutParameters;
        }

        protected override void UpdateView()
        {
            groupId = Item.ViewModel.GroupId;

            timeEntriesLogCellDescription.Text = Item.ViewModel.Description;
            timeEntriesLogCellDescription.Visibility = Item.DescriptionVisibility;
            addDescriptionLabel.Visibility = Item.AddDescriptionLabelVisibility;

            timeEntriesLogCellProjectLabel.TextFormatted = Item.ProjectTaskClientText;
            timeEntriesLogCellProjectLabel.Visibility = Item.ProjectTaskClientVisibility;

            timeEntriesLogCellProjectArchivedIcon.Visibility = Item.ProjectArchivedIconVisibility;
            timeEntriesLogCellProjectArchivedIcon.SetColorFilter(Item.ProjectArchivedIconTintColor);

            timeEntriesLogCellDuration.Text = Item.ViewModel.Duration;

            timeEntriesLogCellContinueImage.Visibility = Item.ContinueImageVisibility;
            errorImageView.Visibility = Item.ErrorImageViewVisibility;
            errorNeedsSync.Visibility = Item.ErrorNeedsSyncVisibility;
            timeEntriesLogCellContinueButton.Visibility = Item.ContinueButtonVisibility;
            billableIcon.Visibility = Item.BillableIconVisibility;
            hasTagsIcon.Visibility = Item.HasTagsIconVisibility;

            durationPadding.LayoutParameters = getDurationPaddingWidthDependentOnIcons();

            switch (Item.ViewModel.VisualizationIntent)
            {
                case LogItemVisualizationIntent.SingleItem:
                    presentAsSingleTimeEntry();
                    break;

                case LogItemVisualizationIntent.GroupItem:
                    presentAsTimeEntryInAGroup();
                    break;

                case LogItemVisualizationIntent.CollapsedGroupHeader:
                    presentAsCollapsedGroupHeader(Item.ViewModel.RepresentedTimeEntriesIds.Length);
                    break;

                case LogItemVisualizationIntent.ExpandedGroupHeader:
                    presentAsExpandedGroupHeader(Item.ViewModel.RepresentedTimeEntriesIds.Length);
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"Cannot visualize {Item.ViewModel.VisualizationIntent} in the time entries log table.");
            }
        }

        private void presentAsCollapsedGroupHeader(int timeEntriesCount)
        {
            groupExpansionButton.Enabled = true;
            groupCountTextView.SetBackgroundResource(Resource.Drawable.GrayBorderRoundedRectangle);
            groupCountTextView.Enabled = true;
            groupCountTextView.Text = timeEntriesCount.ToString();
            groupCountTextView.Visibility = ViewStates.Visible;
            groupCountTextView.SetTextColor(GroupingColor.Collapsed.Text.ToNativeColor());
            groupItemBackground.Visibility = ViewStates.Gone;
            durationPadding.SetBackgroundColor(whiteColor);
            durationFadeGradient.SetBackgroundResource(Resource.Drawable.TransparentToWhiteGradient);
        }

        private void presentAsExpandedGroupHeader(int timeEntriesCount)
        {
            groupExpansionButton.Enabled = true;
            groupCountTextView.SetBackgroundResource(Resource.Drawable.LightBlueRoundedRectangle);
            groupCountTextView.Enabled = true;
            groupCountTextView.Text = timeEntriesCount.ToString();
            groupCountTextView.Visibility = ViewStates.Visible;
            groupCountTextView.SetTextColor(GroupingColor.Expanded.Text.ToNativeColor());
            groupItemBackground.Visibility = ViewStates.Gone;
            durationPadding.SetBackgroundColor(whiteColor);
            durationFadeGradient.SetBackgroundResource(Resource.Drawable.TransparentToWhiteGradient);
        }

        private void presentAsSingleTimeEntry()
        {
            groupExpansionButton.Enabled = false;
            groupCountTextView.Visibility = ViewStates.Gone;
            groupItemBackground.Visibility = ViewStates.Gone;
            durationPadding.SetBackgroundColor(whiteColor);
            durationFadeGradient.SetBackgroundResource(Resource.Drawable.TransparentToWhiteGradient);
        }

        private void presentAsTimeEntryInAGroup()
        {
            groupExpansionButton.Enabled = false;
            groupCountTextView.Visibility = ViewStates.Invisible;
            groupItemBackground.Visibility = ViewStates.Visible;
            durationPadding.SetBackgroundColor(grayColor);
            durationFadeGradient.SetBackgroundResource(Resource.Drawable.TransparentToLightGrayGradient);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            timeEntriesLogCellContinueButton.Click -= onContinueClick;
            groupExpansionButton.Click -= onExpansionClick;
        }
    }
}
