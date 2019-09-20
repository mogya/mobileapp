
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Toggl.Droid.Adapters;
using Toggl.Droid.Views;
using static Toggl.Droid.Resource.Id;

namespace Toggl.Droid.Activities
{
    public partial class StartTimeEntryActivity
    {
        private TextView doneButton;
        private ImageView closeButton;
        private ImageView selectTagToolbarButton;
        private ImageView selectProjectToolbarButton;
        private ImageView selectBillableToolbarButton;

        private TextView durationLabel;

        private RecyclerView recyclerView;

        private AutocompleteEditText descriptionField;
        private StartTimeEntryRecyclerAdapter adapter;

        protected override void InitializeViews()
        {
            closeButton = FindViewById<ImageView>(CloseButton);
            selectTagToolbarButton = FindViewById<ImageView>(ToolbarTagButton);
            selectProjectToolbarButton = FindViewById<ImageView>(ToolbarProjectButton);
            selectBillableToolbarButton = FindViewById<ImageView>(ToolbarBillableButton);

            doneButton = FindViewById<TextView>(DoneButton);
            durationLabel = FindViewById<TextView>(DurationText);

            recyclerView = FindViewById<RecyclerView>(SuggestionsRecyclerView);

            descriptionField = FindViewById<AutocompleteEditText>(DescriptionTextField);
            
            doneButton.Text = Shared.Resources.Done;

            adapter = new StartTimeEntryRecyclerAdapter();
            recyclerView.SetLayoutManager(new LinearLayoutManager(this));
            recyclerView.SetAdapter(adapter);
        }
    }
}
