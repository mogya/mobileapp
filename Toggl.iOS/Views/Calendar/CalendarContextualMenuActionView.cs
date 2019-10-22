using System;
using System.Reactive.Disposables;
using CoreGraphics;
using Toggl.Core.UI.ViewModels.Calendar.ContextualMenu;
using Toggl.iOS.Extensions;
using Toggl.iOS.Extensions.Reactive;
using Toggl.Shared.Extensions;
using UIKit;
using Colors = Toggl.Core.UI.Helper.Colors;

namespace Toggl.iOS.Views.Calendar
{
    public class CalendarContextualMenuActionView : UIButton
    {
        private const int topPadding = 19;
        private const int iconBackgroundWidth = 40;
        private const int spaceBetweenActionNameLabelAndIconBackground = 8;

        private readonly UIImageView iconView;
        private readonly UILabel actionNameLabel;
        private readonly UIView iconBackgroundView;
        private readonly CalendarMenuAction calendarMenuAction;
        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        public override CGSize IntrinsicContentSize => new CGSize(120, 90);

        public CalendarContextualMenuActionView(CalendarMenuAction calendarMenuAction)
        {
            this.calendarMenuAction = calendarMenuAction;
            iconView = new UIImageView
            {
                UserInteractionEnabled = false,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Image = UIImage.FromBundle(iconFor(calendarMenuAction))
            };
            iconBackgroundView = new UIView
            {
                UserInteractionEnabled = false,
                TranslatesAutoresizingMaskIntoConstraints = false,
                BackgroundColor = iconBackgroundViewColorFor(calendarMenuAction)
            };
            actionNameLabel = new UILabel
            {
                UserInteractionEnabled = false,
                Text = calendarMenuAction.Title,
                TextAlignment = UITextAlignment.Center,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Font = UIFont.SystemFontOfSize(11, UIFontWeight.Regular)
            };

            buildView();
        }

        private void buildView()
        {
            AddSubview(iconBackgroundView);
            AddSubview(actionNameLabel);
            iconBackgroundView.AddSubview(iconView);

            iconBackgroundView.Layer.CornerRadius = iconBackgroundWidth / 2;
            iconBackgroundView.TopAnchor.ConstraintEqualTo(TopAnchor, topPadding).Active = true;
            iconBackgroundView.WidthAnchor.ConstraintEqualTo(iconBackgroundWidth).Active = true;
            iconBackgroundView.HeightAnchor.ConstraintEqualTo(iconBackgroundView.WidthAnchor).Active = true;
            iconBackgroundView.CenterXAnchor.ConstraintEqualTo(CenterXAnchor).Active = true;

            iconView.CenterXAnchor.ConstraintEqualTo(iconBackgroundView.CenterXAnchor).Active = true;
            iconView.CenterYAnchor.ConstraintEqualTo(iconBackgroundView.CenterYAnchor).Active = true;

            actionNameLabel.TopAnchor.ConstraintEqualTo(iconBackgroundView.BottomAnchor, spaceBetweenActionNameLabelAndIconBackground).Active = true;
            actionNameLabel.LeadingAnchor.ConstraintEqualTo(LeadingAnchor).Active = true;
            actionNameLabel.TrailingAnchor.ConstraintEqualTo(TrailingAnchor).Active = true;

            this.Rx()
                .BindAction(calendarMenuAction.MenuItemAction)
                .DisposedBy(disposeBag);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;

            disposeBag.Dispose();
        }

        private static UIColor iconBackgroundViewColorFor(CalendarMenuAction action)
        {
            switch (action.ActionKind)
            {
                case CalendarMenuActionKind.Continue:
                    return Colors.Calendar.ContextualMenuAction.Continue.ToNativeColor();
                case CalendarMenuActionKind.Copy:
                    return Colors.Calendar.ContextualMenuAction.Copy.ToNativeColor();
                case CalendarMenuActionKind.Delete:
                    return Colors.Calendar.ContextualMenuAction.Delete.ToNativeColor();
                case CalendarMenuActionKind.Discard:
                    return Colors.Calendar.ContextualMenuAction.Discard.ToNativeColor();
                case CalendarMenuActionKind.Edit:
                    return Colors.Calendar.ContextualMenuAction.Edit.ToNativeColor();
                case CalendarMenuActionKind.Save:
                    return Colors.Calendar.ContextualMenuAction.Save.ToNativeColor();
                case CalendarMenuActionKind.Start:
                    return Colors.Calendar.ContextualMenuAction.Start.ToNativeColor();
                case CalendarMenuActionKind.Stop:
                    return Colors.Calendar.ContextualMenuAction.Stop.ToNativeColor();
                default:
                    throw new ArgumentException($"Unsupported value of {nameof(CalendarMenuAction)}");
            }
        }

        private static string iconFor(CalendarMenuAction action)
        {
            switch (action.ActionKind)
            {
                case CalendarMenuActionKind.Continue:
                    return "continue";
                case CalendarMenuActionKind.Copy:
                    return "copy";
                case CalendarMenuActionKind.Delete:
                    return "delete";
                case CalendarMenuActionKind.Discard:
                    return "discard";
                case CalendarMenuActionKind.Edit:
                    return "edit";
                case CalendarMenuActionKind.Save:
                    return "save";
                case CalendarMenuActionKind.Start:
                    return "continue";
                case CalendarMenuActionKind.Stop:
                    return "stop";
                default:
                    throw new ArgumentException($"Unsupported value of ${nameof(CalendarMenuAction)}");
            }
        }
    }
}
