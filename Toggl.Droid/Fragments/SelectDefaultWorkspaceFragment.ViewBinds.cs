
using Android.Views;
using AndroidX.RecyclerView.Widget;

namespace Toggl.Droid.Fragments
{
    public partial class SelectDefaultWorkspaceFragment
    {
        private RecyclerView recyclerView;

        protected override void InitializeViews(View rootView)
        {
            recyclerView = rootView.FindViewById<RecyclerView>(Resource.Id.SelectDefaultWorkspaceRecyclerView);
        }
    }
}
