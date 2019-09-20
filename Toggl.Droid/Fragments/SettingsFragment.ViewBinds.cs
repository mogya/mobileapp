

using Android.Views;
using Android.Widget;
using AndroidX.Core.Widget;
using Google.Android.Material.AppBar;

namespace Toggl.Droid.Fragments
{
    public sealed partial class SettingsFragment
    {
        private View aboutView;
        private View manualModeView;
        private View is24hoursModeView;
        private View runningTimerNotificationsView;
        private View stoppedTimerNotificationsView;
        private View dateFormatView;
        private View beginningOfWeekView;
        private View durationFormatView;
        private View smartRemindersView;
        private View smartRemindersViewSeparator;
        private View groupTimeEntriesView;
        private View defaultWorkspaceView;

        private TextView helpView;
        private TextView calendarSettingsView;
        private TextView nameTextView;
        private TextView emailTextView;
        private TextView versionTextView;
        private TextView dateFormatTextView;
        private TextView beginningOfWeekTextView;
        private TextView durationFormatTextView;
        private TextView smartRemindersTextView;
        private TextView defaultWorkspaceNameTextView;
        private TextView yourProfileLabel;
        private TextView usernameLabel;
        private TextView emailLabel;
        private TextView defaultWorkspaceLabel;
        private TextView displayLabel;
        private TextView dateFormatLabel;
        private TextView beginningOfWeekLabel;
        private TextView durationFormatLabel;
        private TextView use24HourClockLabel;
        private TextView groupedTimeEntriesLabel;
        private TextView smartRemindersLabel;
        private TextView notificationsLabel;
        private TextView notificationsRunningTimerLabel;
        private TextView notificationsStoppedTimerLabel;
        private TextView generalLabel;
        private TextView aboutLabel;
        private TextView settingsToggleManualModeLabel;
        private TextView settingsToggleManualModeExplanation;
        private TextView feedbackView;
        private TextView logoutView;
        
        private Switch is24hoursModeSwitch;
        private Switch manualModeSwitch;
        private Switch runningTimerNotificationsSwitch;
        private Switch stoppedTimerNotificationsSwitch;
        private Switch groupTimeEntriesSwitch;

        private AppBarLayout appBarLayout;
        private NestedScrollView scrollView;

        protected override void InitializeViews(View fragmentView)
        {
            aboutView = fragmentView.FindViewById(Resource.Id.SettingsAboutContainer);
            manualModeView = fragmentView.FindViewById(Resource.Id.SettingsToggleManualModeView);
            is24hoursModeView = fragmentView.FindViewById(Resource.Id.SettingsIs24HourModeView);
            dateFormatView = fragmentView.FindViewById(Resource.Id.SettingsDateFormatView);
            beginningOfWeekView = fragmentView.FindViewById(Resource.Id.SettingsSelectBeginningOfWeekView);
            durationFormatView = fragmentView.FindViewById(Resource.Id.SettingsDurationFormatView);
            smartRemindersView = fragmentView.FindViewById(Resource.Id.SmartRemindersView);
            smartRemindersViewSeparator = fragmentView.FindViewById(Resource.Id.SmartRemindersViewSeparator);
            runningTimerNotificationsView = fragmentView.FindViewById(Resource.Id.SettingsRunningTimerNotificationsView);
            stoppedTimerNotificationsView = fragmentView.FindViewById(Resource.Id.SettingsStoppedTimerNotificationsView);
            groupTimeEntriesView = fragmentView.FindViewById(Resource.Id.GroupTimeEntriesView);
            defaultWorkspaceView = fragmentView.FindViewById(Resource.Id.DefaultWorkspaceView);

            logoutView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsLogoutButton);
            helpView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsHelpButton);
            feedbackView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsSubmitFeedbackButton);
            calendarSettingsView = fragmentView.FindViewById<TextView>(Resource.Id.CalendarSettingsView);
            nameTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsNameTextView);
            emailTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsEmailTextView);
            versionTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsAppVersionTextView);
            dateFormatTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsDateFormatTextView);
            beginningOfWeekTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsBeginningOfWeekTextView);
            durationFormatTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsDurationFormatTextView);
            smartRemindersTextView = fragmentView.FindViewById<TextView>(Resource.Id.SmartRemindersTextView);
            defaultWorkspaceNameTextView = fragmentView.FindViewById<TextView>(Resource.Id.DefaultWorkspaceName);
            yourProfileLabel = fragmentView.FindViewById<TextView>(Resource.Id.YourProfileLabel);
            usernameLabel = fragmentView.FindViewById<TextView>(Resource.Id.UsernameLabel);
            emailLabel = fragmentView.FindViewById<TextView>(Resource.Id.EmailLabel);
            defaultWorkspaceLabel = fragmentView.FindViewById<TextView>(Resource.Id.DefaultWorkspaceLabel);
            displayLabel = fragmentView.FindViewById<TextView>(Resource.Id.DisplayLabel);
            dateFormatLabel = fragmentView.FindViewById<TextView>(Resource.Id.DateFormatLabel);
            beginningOfWeekLabel = fragmentView.FindViewById<TextView>(Resource.Id.BeginningOfWeekLabel);
            durationFormatLabel = fragmentView.FindViewById<TextView>(Resource.Id.DurationFormatLabel);
            use24HourClockLabel = fragmentView.FindViewById<TextView>(Resource.Id.Use24HourClockLabel);
            groupedTimeEntriesLabel = fragmentView.FindViewById<TextView>(Resource.Id.GroupedTimeEntriesLabel);
            smartRemindersLabel = fragmentView.FindViewById<TextView>(Resource.Id.SmartRemindersLabel);
            notificationsLabel = fragmentView.FindViewById<TextView>(Resource.Id.NotificationsLabel);
            notificationsRunningTimerLabel = fragmentView.FindViewById<TextView>(Resource.Id.NotificationsRunningTimerLabel);
            notificationsStoppedTimerLabel = fragmentView.FindViewById<TextView>(Resource.Id.NotificationsStoppedTimerLabel);
            generalLabel = fragmentView.FindViewById<TextView>(Resource.Id.GeneralLabel);
            aboutLabel = fragmentView.FindViewById<TextView>(Resource.Id.AboutLabel);
            settingsToggleManualModeLabel = fragmentView.FindViewById<TextView>(Resource.Id.SettingsToggleManualModeLabel);
            settingsToggleManualModeExplanation = fragmentView.FindViewById<TextView>(Resource.Id.SettingsToggleManualModeExplanation);

            manualModeSwitch = fragmentView.FindViewById<Switch>(Resource.Id.SettingsIsManualModeEnabledSwitch);
            is24hoursModeSwitch = fragmentView.FindViewById<Switch>(Resource.Id.SettingsIs24HourModeSwitch);
            runningTimerNotificationsSwitch = fragmentView.FindViewById<Switch>(Resource.Id.SettingsAreRunningTimerNotificationsEnabledSwitch);
            stoppedTimerNotificationsSwitch = fragmentView.FindViewById<Switch>(Resource.Id.SettingsAreStoppedTimerNotificationsEnabledSwitch);
            groupTimeEntriesSwitch = fragmentView.FindViewById<Switch>(Resource.Id.GroupTimeEntriesSwitch);

            appBarLayout = fragmentView.FindViewById<AppBarLayout>(Resource.Id.AppBarLayout);

            scrollView = fragmentView.FindViewById<NestedScrollView>(Resource.Id.ScrollView);
            
            logoutView.Text = Shared.Resources.SignOutOfToggl;
            helpView.Text = Shared.Resources.Help;
            feedbackView.Text = Shared.Resources.SubmitFeedback;
            calendarSettingsView.Text = Shared.Resources.CalendarSettingsTitle;
            yourProfileLabel.Text = Shared.Resources.YourProfile;
            usernameLabel.Text = Shared.Resources.Username;
            emailLabel.Text = Shared.Resources.Email;
            defaultWorkspaceLabel.Text = Shared.Resources.DefaultWorkspace;
            displayLabel.Text = Shared.Resources.Display;
            dateFormatLabel.Text = Shared.Resources.DateFormat;
            beginningOfWeekLabel.Text = Shared.Resources.FirstDayOfTheWeek;
            durationFormatLabel.Text = Shared.Resources.DurationFormat;
            use24HourClockLabel.Text = Shared.Resources.Use24HourClock;
            groupedTimeEntriesLabel.Text = Shared.Resources.GroupTimeEntries;
            smartRemindersLabel.Text = Shared.Resources.SmartReminders;
            notificationsLabel.Text = Shared.Resources.Notifications;
            notificationsRunningTimerLabel.Text = Shared.Resources.NotificationsRunningTimer;
            notificationsStoppedTimerLabel.Text = Shared.Resources.NotificationsStoppedTimer;
            generalLabel.Text = Shared.Resources.General;
            aboutLabel.Text = Shared.Resources.About;
            settingsToggleManualModeLabel.Text = Shared.Resources.ManualMode;
            settingsToggleManualModeExplanation.Text = Shared.Resources.ManualModeDescription;
        }
    }
}
