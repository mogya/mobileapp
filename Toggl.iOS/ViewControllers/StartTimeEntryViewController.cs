using CoreGraphics;
using Foundation;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Toggl.Core.Autocomplete;
using Toggl.Core.Autocomplete.Suggestions;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.Helper;
using Toggl.Core.UI.Onboarding.CreationView;
using Toggl.Core.UI.Onboarding.StartTimeEntryView;
using Toggl.Core.UI.ViewModels;
using Toggl.iOS.Autocomplete;
using Toggl.iOS.Extensions;
using Toggl.iOS.Extensions.Reactive;
using Toggl.iOS.ViewSources;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using UIKit;

namespace Toggl.iOS.ViewControllers
{
    public sealed partial class StartTimeEntryViewController : KeyboardAwareViewController<StartTimeEntryViewModel>
    {
        private const double desiredIpadHeight = 360;

        private bool isUpdatingDescriptionField;

        private UIImage greyCheckmarkButtonImage;
        private UIImage greenCheckmarkButtonImage;

        private IDisposable descriptionDisposable;
        private IDisposable addProjectOrTagOnboardingDisposable;
        private IDisposable disabledConfirmationButtonOnboardingDisposable;

        private ISubject<bool> isDescriptionEmptySubject = new BehaviorSubject<bool>(true);

        private IUITextInputDelegate emptyInputDelegate = new EmptyInputDelegate();

        public StartTimeEntryViewController(StartTimeEntryViewModel viewModel)
            : base(viewModel, nameof(StartTimeEntryViewController))
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            descriptionDisposable?.Dispose();
            descriptionDisposable = null;

            addProjectOrTagOnboardingDisposable?.Dispose();
            addProjectOrTagOnboardingDisposable = null;

            disabledConfirmationButtonOnboardingDisposable?.Dispose();
            disabledConfirmationButtonOnboardingDisposable = null;

            TimeInput.LostFocus -= onTimeInputLostFocus;
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular)
            {
                View.ClipsToBounds = true;
            }
        }

        public override void ViewWillLayoutSubviews()
        {
            base.ViewWillLayoutSubviews();

            if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular)
            {
                View.ClipsToBounds = true;
            }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            CloseButton.SetTemplateColor(ColorAssets.Text2);

            BottomOptionsSheet.InsertSeparator(UIRectEdge.Top);

            AddProjectBubbleLabel.Text = Resources.AddProjectBubbleText;

            prepareViews();
            prepareOnboarding();

            var source = new StartTimeEntryTableViewSource(SuggestionsTableView);
            SuggestionsTableView.Source = source;

            source.Rx().ModelSelected()
                .Subscribe(ViewModel.SelectSuggestion.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.Suggestions
                .Subscribe(SuggestionsTableView.Rx().ReloadSections(source))
                .DisposedBy(DisposeBag);

            source.ToggleTasks
                .Subscribe(ViewModel.ToggleTasks.Inputs)
                .DisposedBy(DisposeBag);

            TimeInput.Rx().Duration()
                .Subscribe(ViewModel.SetRunningTime.Inputs)
                .DisposedBy(DisposeBag);

            //Text

            ViewModel.DisplayedTime
                .Subscribe(TimeLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            Placeholder.Text = ViewModel.PlaceholderText;

            // Buttons
            UIColor booleanToColor(bool b) => b
                ? Colors.StartTimeEntry.ActiveButton.ToNativeColor()
                : Colors.StartTimeEntry.InactiveButton.ToNativeColor();

            ViewModel.IsBillable
                .Select(booleanToColor)
                .Subscribe(BillableButton.Rx().TintColor())
                .DisposedBy(DisposeBag);

            ViewModel.IsSuggestingTags
                .Select(booleanToColor)
                .Subscribe(TagsButton.Rx().TintColor())
                .DisposedBy(DisposeBag);

            ViewModel.IsSuggestingProjects
                .Select(booleanToColor)
                .Subscribe(ProjectsButton.Rx().TintColor())
                .DisposedBy(DisposeBag);

            //Visibility
            ViewModel.IsBillableAvailable
                .Select(b => b ? (nfloat)42 : 0)
                .Subscribe(BillableButtonWidthConstraint.Rx().Constant())
                .DisposedBy(DisposeBag);

            // Actions
            CloseButton.Rx().Tap()
                .Subscribe(ViewModel.CloseWithDefaultResult)
                .DisposedBy(DisposeBag);

            DoneButton.Rx()
                .BindAction(ViewModel.Done)
                .DisposedBy(DisposeBag);

            ViewModel.Done.Elements
                .Subscribe(IosDependencyContainer.Instance.IntentDonationService.DonateStartTimeEntry)
                .DisposedBy(DisposeBag);

            BillableButton.Rx()
                .BindAction(ViewModel.ToggleBillable)
                .DisposedBy(DisposeBag);

            StartDateButton.Rx()
                .BindAction(ViewModel.SetStartDate)
                .DisposedBy(DisposeBag);

            DateTimeButton.Rx()
                .BindAction(ViewModel.ChangeTime)
                .DisposedBy(DisposeBag);

            TagsButton.Rx()
                .BindAction(ViewModel.ToggleTagSuggestions)
                .DisposedBy(DisposeBag);

            ProjectsButton.Rx()
                .BindAction(ViewModel.ToggleProjectSuggestions)
                .DisposedBy(DisposeBag);

            // Reactive
            ViewModel.TextFieldInfo
                .DistinctUntilChanged()
                .Subscribe(onTextFieldInfo)
                .DisposedBy(DisposeBag);

            DescriptionTextView.Rx().AttributedText()
                .Select(attributedString => attributedString.Length == 0)
                .Subscribe(isDescriptionEmptySubject)
                .DisposedBy(DisposeBag);

            Observable.CombineLatest(
                    DescriptionTextView.Rx().AttributedText().SelectUnit(),
                    DescriptionTextView.Rx().CursorPosition().SelectUnit()
                )
                .Select(_ => DescriptionTextView.AttributedText) // Programatically changing the text doesn't send an event, that's why we do this, to get the last version of the text
                .Do(updatePlaceholder)
                .Select(text => text.AsSpans((int)DescriptionTextView.SelectedRange.Location).ToIImmutableList())
                .Subscribe(ViewModel.SetTextSpans.Inputs)
                .DisposedBy(DisposeBag);
        }

        private void onTextFieldInfo(TextFieldInfo textFieldInfo)
        {
            var attributedText = textFieldInfo.AsAttributedTextAndCursorPosition();
            if (DescriptionTextView.AttributedText.GetHashCode() == attributedText.GetHashCode())
                return;

            //This line is needed for when the user selects from suggestion and
            // the iOS autocorrect is ready to add text at the same time.
            // Without this line both will happen.
            DescriptionTextView.InputDelegate = emptyInputDelegate;
            DescriptionTextView.AttributedText = attributedText;

            DescriptionTextView.RejectAutocorrect();

            updatePlaceholder();
        }

        private void switchTimeLabelAndInput()
        {
            TimeLabel.Hidden = !TimeLabel.Hidden;
            TimeInput.Hidden = !TimeInput.Hidden;

            TimeLabelTrailingConstraint.Active = !TimeLabel.Hidden;
            TimeInputTrailingConstraint.Active = !TimeInput.Hidden;
        }

        private void updatePlaceholder(NSAttributedString text = null)
        {
            Placeholder.UpdateVisibility(DescriptionTextView);
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            BottomDistanceConstraint.Constant = e.FrameEnd.Height;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            BottomDistanceConstraint.Constant = 0;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            base.TouchesBegan(touches, evt);

            if (TimeInput.IsEditing)
            {
                TimeInput.EndEditing(true);
                DescriptionTextView.BecomeFirstResponder();
            }
        }

        private void prepareViews()
        {
            if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular)
            {
                PreferredContentSize = new CGSize(0, desiredIpadHeight);
            }

            //This is needed for the ImageView.TintColor bindings to work
            foreach (var button in getButtons())
            {
                button.SetImage(
                    button.ImageForState(UIControlState.Normal)
                          .ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate),
                    UIControlState.Normal
                );
                button.TintColor = Colors.StartTimeEntry.InactiveButton.ToNativeColor();
            }

            TimeInput.TintColor = Colors.StartTimeEntry.Cursor.ToNativeColor();

            DescriptionTextView.TintColor = Colors.StartTimeEntry.Cursor.ToNativeColor();
            DescriptionTextView.BecomeFirstResponder();

            Placeholder.ConfigureWith(DescriptionTextView);
            Placeholder.Text = Resources.StartTimeEntryPlaceholder;

            prepareTimeViews();
        }

        private void prepareTimeViews()
        {
            var tapRecognizer = new UITapGestureRecognizer(() =>
            {
                if (!TimeLabel.Hidden)
                    ViewModel.DurationTapped.Execute();

                switchTimeLabelAndInput();

                if (!TimeInput.Hidden)
                {
                    TimeInput.FormattedDuration = TimeLabel.Text;
                    TimeInput.BecomeFirstResponder();
                }
            });

            TimeLabel.UserInteractionEnabled = true;
            TimeLabel.AddGestureRecognizer(tapRecognizer);

            TimeInput.LostFocus += onTimeInputLostFocus;
        }

        private void onTimeInputLostFocus(object sender, EventArgs e)
            => switchTimeLabelAndInput();

        private IEnumerable<UIButton> getButtons()
        {
            yield return TagsButton;
            yield return ProjectsButton;
            yield return BillableButton;
            yield return StartDateButton;
            yield return DateTimeButton;
            yield return DateTimeButton;
        }

        private void toggleTaskSuggestions(ProjectSuggestion parameter)
        {
            var offset = SuggestionsTableView.ContentOffset;
            var frameHeight = SuggestionsTableView.Frame.Height;

            ViewModel.ToggleTasks.Execute(parameter);

            SuggestionsTableView.CorrectOffset(offset, frameHeight);
        }

        private void prepareOnboarding()
        {
            prepareAddProjectOnboardingStep();
            prepareDisableConfirmationButtonOnboardingStep();
        }

        private void prepareAddProjectOnboardingStep()
        {
            var onboardingStorage = ViewModel.OnboardingStorage;
            var addProjectOrtagOnboardingStep = new AddProjectOrTagOnboardingStep(
                onboardingStorage,
                ViewModel.DataSource
            );

            addProjectOrTagOnboardingDisposable = addProjectOrtagOnboardingStep
                .ManageDismissableTooltip(AddProjectOnboardingBubble, onboardingStorage);
        }

        private void prepareDisableConfirmationButtonOnboardingStep()
        {
            greyCheckmarkButtonImage = UIImage.FromBundle("icCheckGrey");
            greenCheckmarkButtonImage = UIImage.FromBundle("doneGreen");

            var disabledConfirmationButtonOnboardingStep
                = new DisabledConfirmationButtonOnboardingStep(
                    ViewModel.OnboardingStorage,
                    isDescriptionEmptySubject.AsObservable());

            disabledConfirmationButtonOnboardingDisposable
                = disabledConfirmationButtonOnboardingStep
                    .ShouldBeVisible
                    .ObserveOn(IosDependencyContainer.Instance.SchedulerProvider.MainScheduler)
                    .Subscribe(visible =>
                    {
                        var image = visible ? greyCheckmarkButtonImage : greenCheckmarkButtonImage;
                        DoneButton.SetImage(image, UIControlState.Normal);
                    });
        }
    }
}

