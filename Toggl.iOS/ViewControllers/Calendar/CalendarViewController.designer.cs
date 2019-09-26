// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Toggl.iOS.ViewControllers.Calendar
{
	[Register ("CalendarViewController")]
	partial class CalendarViewController
	{
		[Outlet]
		UIKit.UILabel DailyTrackedTimeLabel { get; set; }

		[Outlet]
		UIKit.UIView DayViewContainer { get; set; }

		[Outlet]
		UIKit.UILabel SelectedDateLabel { get; set; }

		[Outlet]
		UIKit.UIButton SettingsButton { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (DailyTrackedTimeLabel != null) {
				DailyTrackedTimeLabel.Dispose ();
				DailyTrackedTimeLabel = null;
			}

			if (DayViewContainer != null) {
				DayViewContainer.Dispose ();
				DayViewContainer = null;
			}

			if (SelectedDateLabel != null) {
				SelectedDateLabel.Dispose ();
				SelectedDateLabel = null;
			}

			if (SettingsButton != null) {
				SettingsButton.Dispose ();
				SettingsButton = null;
			}
		}
	}
}
