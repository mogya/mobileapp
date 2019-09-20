
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Toggl.Droid.Adapters;

namespace Toggl.Droid.Fragments
{
    public sealed partial class SelectUserCalendarsFragment
    {
        private TextView titleLabel;
        private TextView messageLabel;
        private Button cancelButton;
        private Button doneButton;
        private RecyclerView recyclerView;

        protected override void InitializeViews(View view)
        {
            titleLabel = view.FindViewById<TextView>(Resource.Id.SelectCalendarsTitle);
            messageLabel = view.FindViewById<TextView>(Resource.Id.SelectCalendarsMessage);
            cancelButton = view.FindViewById<Button>(Resource.Id.CancelButton);
            doneButton = view.FindViewById<Button>(Resource.Id.DoneButton);
            recyclerView = view.FindViewById<RecyclerView>(Resource.Id.CalendarsRecyclerView);
            
            titleLabel.Text = Shared.Resources.SelectCalendars;
            messageLabel.Text = Shared.Resources.SelectCalendarsMessage;
            cancelButton.Text = Shared.Resources.Cancel;
            doneButton.Text = Shared.Resources.Done;
            
            userCalendarsAdapter = new UserCalendarsRecyclerAdapter();
            recyclerView.SetAdapter(userCalendarsAdapter);
            recyclerView.SetLayoutManager(new LinearLayoutManager(Context));
        }
    }
}
