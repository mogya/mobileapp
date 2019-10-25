using Android.Support.Design.Widget;
using Android.Views;

namespace Toggl.Droid.Activities
{
    public sealed partial class MainTabBarActivity
    {
        private BottomNavigationView navigationView;

        protected override void InitializeViews()
        {
            navigationView = FindViewById<BottomNavigationView>(Resource.Id.MainTabBarBottomNavigationView);

            var menu = navigationView.Menu;
            var timerTab = menu.FindItem(Resource.Id.MainTabTimerItem);
            timerTab.SetTitle(Shared.Resources.Timer);

            var reportsTab = menu.FindItem(Resource.Id.MainTabReportsItem);
            reportsTab.SetTitle(Shared.Resources.Reports);

            var calendarTab = menu.FindItem(Resource.Id.MainTabCalendarItem);
            calendarTab.SetTitle(Shared.Resources.Calendar);
        }
    }
}
