
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Toggl.Droid.Adapters;

namespace Toggl.Droid.Fragments
{
    public sealed partial class SelectDateFormatFragment
    {
        private TextView titleLabel;
        private RecyclerView recyclerView;
        private SelectDateFormatRecyclerAdapter selectDateRecyclerAdapter;

        protected override void InitializeViews(View rootView)
        {
            titleLabel = rootView.FindViewById<TextView>(Resource.Id.SelectDateFormatTitle);
            recyclerView = rootView.FindViewById<RecyclerView>(Resource.Id.SelectDateFormatRecyclerView);
            
            titleLabel.Text = Shared.Resources.DateFormat;

            recyclerView.SetLayoutManager(new LinearLayoutManager(Context));
            selectDateRecyclerAdapter = new SelectDateFormatRecyclerAdapter();
            recyclerView.SetAdapter(selectDateRecyclerAdapter);
        }
    }
}
