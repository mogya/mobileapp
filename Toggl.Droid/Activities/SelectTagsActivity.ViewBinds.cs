
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Toggl.Droid.Adapters;

namespace Toggl.Droid.Activities
{
    public partial class SelectTagsActivity
    {
        private readonly SelectTagsRecyclerAdapter selectTagsRecyclerAdapter = new SelectTagsRecyclerAdapter();

        private ImageView backIcon;
        private ImageView clearIcon;
        private TextView saveButton;
        private EditText textField;
        private RecyclerView selectTagsRecyclerView;

        protected override void InitializeViews()
        {
            backIcon = FindViewById<ImageView>(Resource.Id.BackIcon);
            clearIcon = FindViewById<ImageView>(Resource.Id.ClearIcon);
            saveButton = FindViewById<TextView>(Resource.Id.SaveButton);
            textField = FindViewById<EditText>(Resource.Id.TextField);
            selectTagsRecyclerView = FindViewById<RecyclerView>(Resource.Id.SelectTagsRecyclerView);

            textField.Hint = Shared.Resources.AddTags;
            saveButton.Text = Shared.Resources.Done;
            var layoutManager = new LinearLayoutManager(this);
            layoutManager.ItemPrefetchEnabled = true;
            layoutManager.InitialPrefetchItemCount = 4;
            selectTagsRecyclerView.SetLayoutManager(layoutManager);
            selectTagsRecyclerView.SetAdapter(selectTagsRecyclerAdapter);
        }
    }
}
