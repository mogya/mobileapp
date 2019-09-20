
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Toggl.Core.UI.ViewModels;
using Toggl.Droid.Adapters;
using Toggl.Droid.ViewHolders;

namespace Toggl.Droid.Fragments
{
    public partial class SelectWorkspaceFragment
    {
        private TextView titleLabel;
        private RecyclerView recyclerView;
        private SimpleAdapter<SelectableWorkspaceViewModel> selectableWorkspaceAdapter;

        protected override void InitializeViews(View rootView)
        {
            titleLabel = rootView.FindViewById<TextView>(Resource.Id.SelectWorkspaceTitle);
            recyclerView = rootView.FindViewById<RecyclerView>(Resource.Id.SelectWorkspaceRecyclerView);
            
            titleLabel.Text = Shared.Resources.Workspace;
            selectableWorkspaceAdapter = new SimpleAdapter<SelectableWorkspaceViewModel>(
                Resource.Layout.SelectWorkspaceFragmentCell,
                SelectWorkspaceViewHolder.Create
            );
            recyclerView.SetAdapter(selectableWorkspaceAdapter);
            recyclerView.SetLayoutManager(new LinearLayoutManager(Context));
        }
    }
}
