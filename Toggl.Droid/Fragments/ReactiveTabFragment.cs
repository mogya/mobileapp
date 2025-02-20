using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Toggl.Core.UI.ViewModels;
using Toggl.Core.UI.Views;

namespace Toggl.Droid.Fragments
{
    public abstract partial class ReactiveTabFragment<TViewModel> : Fragment, IView
        where TViewModel : class, IViewModel
    {
        protected CompositeDisposable DisposeBag = new CompositeDisposable();

        public TViewModel ViewModel { get; set; }

        protected abstract void InitializeViews(View view);

        protected ReactiveTabFragment()
        {
        }

        protected ReactiveTabFragment(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            ViewModel?.AttachView(this);
        }

        public override void OnStart()
        {
            base.OnStart();
            ViewModel?.ViewAppearing();
        }

        public override void OnResume()
        {
            base.OnResume();

            if (IsHidden) return;

            ViewModel?.ViewAppeared();
        }

        public override void OnPause()
        {
            base.OnPause();
            ViewModel?.ViewDisappearing();
        }

        public override void OnStop()
        {
            base.OnStop();
            ViewModel?.ViewDisappeared();
        }

        public override void OnDestroyView()
        {
            ViewModel?.DetachView();
            base.OnDestroyView();
            DisposeBag.Dispose();
            DisposeBag = new CompositeDisposable();
        }

        public override void OnHiddenChanged(bool hidden)
        {
            base.OnHiddenChanged(hidden);
            if (hidden)
                ViewModel?.ViewDisappeared();
            else
                ViewModel?.ViewAppeared();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;
            DisposeBag?.Dispose();
        }

        public void SetupToolbar(View fragmentView, string title = "")
        {
            var activity = Activity as AppCompatActivity;
            var toolbar = fragmentView.FindViewById<Toolbar>(Resource.Id.Toolbar);
            toolbar.Title = title;
            activity.SetSupportActionBar(toolbar);

            activity.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
            activity.SupportActionBar.SetDisplayShowHomeEnabled(false);
        }

        public void Close()
        {
        }

        public IObservable<string> GetGoogleToken()
        {
            if (!(Activity is IGoogleTokenProvider tokenProvider))
                throw new InvalidOperationException();

            return tokenProvider.GetGoogleToken();
        }
    }
}
