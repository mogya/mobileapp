
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Toggl.Droid.Adapters;

namespace Toggl.Droid.Activities
{
    public partial class SelectClientActivity
    {
        private readonly SelectClientRecyclerAdapter selectClientRecyclerAdapter = new SelectClientRecyclerAdapter();

        private ImageView backImageView;
        private EditText filterEditText;
        private RecyclerView selectClientRecyclerView;

        protected override void InitializeViews()
        {
            backImageView = FindViewById<ImageView>(Resource.Id.BackImageView);
            filterEditText = FindViewById<EditText>(Resource.Id.FilterEditText);
            selectClientRecyclerView = FindViewById<RecyclerView>(Resource.Id.SelectClientRecyclerView);

            filterEditText.Hint = Shared.Resources.AddClient;
            selectClientRecyclerView.SetLayoutManager(new LinearLayoutManager(this)
            {
                ItemPrefetchEnabled = true,
                InitialPrefetchItemCount = 4
            });
            selectClientRecyclerView.SetAdapter(selectClientRecyclerAdapter);
        }
    }
}
