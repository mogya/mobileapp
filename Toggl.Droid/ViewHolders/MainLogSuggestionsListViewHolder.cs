using System;
using System.Reactive.Disposables;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Toggl.Core.Suggestions;
using Toggl.Core.UI.ViewModels;
using Toggl.Droid.Adapters;
using Toggl.Shared.Extensions;
using TogglResources = Toggl.Shared.Resources;
using Toggl.Droid.Extensions.Reactive;

namespace Toggl.Droid.ViewHolders
{
    public class MainLogSuggestionsListViewHolder : RecyclerView.ViewHolder
    {
        private SuggestionsViewModel suggestionsViewModel;

        private TextView hintTextView;
        private TextView indicatorTextView;
        private RecyclerView suggestionsRecyclerView;

        public CompositeDisposable DisposeBag { get; } = new CompositeDisposable();

        public MainLogSuggestionsListViewHolder(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public MainLogSuggestionsListViewHolder(View itemView, SuggestionsViewModel suggestionsViewModel) : base(itemView)
        {
            this.suggestionsViewModel = suggestionsViewModel;

            hintTextView = ItemView.FindViewById<TextView>(Resource.Id.SuggestionsHintTextView);
            indicatorTextView = ItemView.FindViewById<TextView>(Resource.Id.SuggestionsIndicatorTextView);
            suggestionsRecyclerView = ItemView.FindViewById<RecyclerView>(Resource.Id.SuggestionsRecyclerView);

            var adapter = new SimpleAdapter<Suggestion>(Resource.Layout.MainSuggestionsCard, MainLogSuggestionItemViewHolder.Create);
            suggestionsRecyclerView.SetLayoutManager(new LinearLayoutManager(ItemView.Context));
            suggestionsRecyclerView.SetAdapter(adapter);

            hintTextView.Text = Shared.Resources.WorkingOnThese;

            adapter.ItemTapObservable
                .Subscribe(suggestionsViewModel.StartTimeEntry.Inputs)
                .DisposedBy(DisposeBag);

            suggestionsViewModel.Suggestions
                .Subscribe(adapter.Rx().Items())
                .DisposedBy(DisposeBag);

            suggestionsViewModel.IsEmpty
                .Invert()
                .Subscribe(updateViewVisibility)
                .DisposedBy(DisposeBag);
        }

        private void updateViewVisibility(bool visible)
        {
            if (visible)
            {
                hintTextView.Visibility = ViewStates.Visible;
                suggestionsRecyclerView.Visibility = ViewStates.Visible;
            }
            else
            {
                hintTextView.Visibility = ViewStates.Gone;
                indicatorTextView.Visibility = ViewStates.Gone;
                suggestionsRecyclerView.Visibility = ViewStates.Gone;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            DisposeBag.Dispose();
        }

    }
}
