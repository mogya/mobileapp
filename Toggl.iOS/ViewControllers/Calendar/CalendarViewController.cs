using System;
using Toggl.Core.UI.ViewModels.Calendar;
using UIKit;

namespace Toggl.iOS.ViewControllers.Calendar
{
    public sealed partial class CalendarViewController : ReactiveViewController<CalendarViewModel>
    {
        public CalendarViewController(CalendarViewModel calendarViewModel)
            : base(calendarViewModel, nameof(CalendarViewController))
        {
        }
    }
}
