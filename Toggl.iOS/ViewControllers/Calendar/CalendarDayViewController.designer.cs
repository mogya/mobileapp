// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Toggl.iOS.ViewControllers
{
	[Register ("CalendarDayViewController")]
	partial class CalendarDayViewController
	{
		[Outlet]
		UIKit.UICollectionView CalendarCollectionView { get; set; }

		[Outlet]
		UIKit.UIView ContextualMenu { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint ContextualMenuBottonConstraint { get; set; }

		[Outlet]
		UIKit.UIButton ContextualMenuCloseButton { get; set; }

		[Outlet]
		UIKit.UIStackView ContextualMenuStackView { get; set; }

		[Outlet]
		UIKit.UILabel ContextualMenuTimeEntryDescriptionProjectTaskClientLabel { get; set; }

		[Outlet]
		UIKit.UILabel ContextualMenuTimeEntryPeriodLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (CalendarCollectionView != null) {
				CalendarCollectionView.Dispose ();
				CalendarCollectionView = null;
			}

			if (ContextualMenu != null) {
				ContextualMenu.Dispose ();
				ContextualMenu = null;
			}

			if (ContextualMenuCloseButton != null) {
				ContextualMenuCloseButton.Dispose ();
				ContextualMenuCloseButton = null;
			}

			if (ContextualMenuStackView != null) {
				ContextualMenuStackView.Dispose ();
				ContextualMenuStackView = null;
			}

			if (ContextualMenuTimeEntryDescriptionProjectTaskClientLabel != null) {
				ContextualMenuTimeEntryDescriptionProjectTaskClientLabel.Dispose ();
				ContextualMenuTimeEntryDescriptionProjectTaskClientLabel = null;
			}

			if (ContextualMenuTimeEntryPeriodLabel != null) {
				ContextualMenuTimeEntryPeriodLabel.Dispose ();
				ContextualMenuTimeEntryPeriodLabel = null;
			}

			if (ContextualMenuBottonConstraint != null) {
				ContextualMenuBottonConstraint.Dispose ();
				ContextualMenuBottonConstraint = null;
			}
		}
	}
}
