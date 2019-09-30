using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Droid.Views;
using Toggl.Droid.Views.Calendar;

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
        private CalendarDayView calendarDayView;
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
            calendarDayView = view.FindViewById<CalendarDayView>(Resource.Id.calendarDayView);
            onboardingViewStub = view.FindViewById<ViewStub>(Resource.Id.OnboardingViewStub);
            appBarLayout = view.FindViewById<AppBarLayout>(Resource.Id.HeaderView);
            
            headerCalendarEventsLabel.Text = Shared.Resources.CalendarEvents;
            headerCalendarTimeEntriesLabel.Text = Shared.Resources.TimeEntries;
            headerLinkCalendarsButton.Text = Shared.Resources.LinkCalendars;
        }
    }
}
