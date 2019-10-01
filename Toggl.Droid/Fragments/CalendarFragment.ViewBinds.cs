using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Droid.Views.Calendar;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Droid.Fragments
{
    public partial class CalendarFragment
    {
        private TextView headerDateTextView;
        private TextView headerTimeEntriesDurationTextView;
        private CalendarDayView calendarDayView;
        private AppBarLayout appBarLayout;
        private Toolbar toolbar;

        protected override void InitializeViews(View view)
        {
            headerDateTextView = view.FindViewById<TextView>(Resource.Id.HeaderDateTextView);
            headerTimeEntriesDurationTextView = view.FindViewById<TextView>(Resource.Id.HeaderTimeEntriesDurationTextView);
            calendarDayView = view.FindViewById<CalendarDayView>(Resource.Id.calendarDayView);
            appBarLayout = view.FindViewById<AppBarLayout>(Resource.Id.HeaderView);
            toolbar = view.FindViewById<Toolbar>(Resource.Id.Toolbar);
        }

        private void setupToolbar()
        {
            //todo: setup on settings toolbars revamp issue
            toolbar.InflateMenu(Resource.Menu.CalendarFragmentMenu);
            var saveMenuItem = toolbar.Menu.FindItem(Resource.Id.Settings);
            saveMenuItem.SetTitle(Shared.Resources.Settings);
        }
    }
}
