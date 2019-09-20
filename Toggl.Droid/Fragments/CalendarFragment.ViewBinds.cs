
using Android.Views;
using Android.Widget;
using Google.Android.Material.AppBar;
using Toggl.Droid.Views;

namespace Toggl.Droid.Fragments
{
    public partial class CalendarFragment
    {
        private View headerCalendarEventsView;
        private View headerTimeEntriesView;
        private TextView headerDayTextView;
        private TextView headerWeekdayTextView;
        private TextView headerCalendarEventsTextView;
        private TextView headerTimeEntriesTextView;
        private TextView headerCalendarEventsLabel;
        private TextView headerCalendarTimeEntriesLabel;
        private Button headerLinkCalendarsButton;
        private CalendarRecyclerView calendarRecyclerView;
        private ViewStub onboardingViewStub;
        private AppBarLayout appBarLayout;
        private View onboardingView;
        private TextView onboardingTitleView;
        private TextView onboardingMessageView;
        private Button getStartedButton;
        private TextView skipButton;

        protected override void InitializeViews(View view)
        {
            headerCalendarEventsView = view.FindViewById(Resource.Id.HeaderCalendarEventsView);
            headerTimeEntriesView = view.FindViewById(Resource.Id.HeaderTimeEntriesView);
            headerDayTextView = view.FindViewById<TextView>(Resource.Id.Day);
            headerWeekdayTextView = view.FindViewById<TextView>(Resource.Id.Weekday);
            headerCalendarEventsTextView = view.FindViewById<TextView>(Resource.Id.HeaderCalendarEventsTextView);
            headerCalendarEventsLabel = view.FindViewById<TextView>(Resource.Id.HeaderCalendarEventsLabel);
            headerCalendarTimeEntriesLabel = view.FindViewById<TextView>(Resource.Id.CalendarHeaderTimeEntriesLabel);
            headerTimeEntriesTextView = view.FindViewById<TextView>(Resource.Id.HeaderTimeEntriesTextView);
            headerLinkCalendarsButton = view.FindViewById<Button>(Resource.Id.HeaderLinkCalendarsButton);
            calendarRecyclerView = view.FindViewById<CalendarRecyclerView>(Resource.Id.calendarRecyclerView);
            onboardingViewStub = view.FindViewById<ViewStub>(Resource.Id.OnboardingViewStub);
            appBarLayout = view.FindViewById<AppBarLayout>(Resource.Id.HeaderView);
            
            headerCalendarEventsLabel.Text = Shared.Resources.CalendarEvents;
            headerCalendarTimeEntriesLabel.Text = Shared.Resources.TimeEntries;
            headerLinkCalendarsButton.Text = Shared.Resources.LinkCalendars;
        }
    }
}
