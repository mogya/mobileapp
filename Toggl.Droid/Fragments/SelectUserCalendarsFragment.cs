using Android.OS;

using Android.Views;
using System;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.ViewModels.Calendar;
using Toggl.Droid.Adapters;
using Toggl.Droid.Extensions.Reactive;
using Toggl.Shared.Extensions;

namespace Toggl.Droid.Fragments
{
    public sealed partial class SelectUserCalendarsFragment : ReactiveDialogFragment<SelectUserCalendarsViewModel>
    {
        private UserCalendarsRecyclerAdapter userCalendarsAdapter;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);

            var contextThemeWrapper = new ContextThemeWrapper(Activity, Resource.Style.TogglDialog);
            var wrappedInflater = inflater.CloneInContext(contextThemeWrapper);

            var view = wrappedInflater.Inflate(Resource.Layout.SelectUserCalendarsFragment, container, false);
            InitializeViews(view);

            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            cancelButton.Rx().Tap()
                .Subscribe(ViewModel.CloseWithDefaultResult)
                .DisposedBy(DisposeBag);

            doneButton
                .Rx()
                .BindAction(ViewModel.Save)
                .DisposedBy(DisposeBag);

            ViewModel.Calendars
                .Subscribe(userCalendarsAdapter.Rx().Items())
                .DisposedBy(DisposeBag);

            userCalendarsAdapter
                .ItemTapObservable
                .Subscribe(ViewModel.SelectCalendar.Inputs)
                .DisposedBy(DisposeBag);
        }

        public override void OnResume()
        {
            base.OnResume();
            var layoutParams = Dialog.Window.Attributes;
            layoutParams.Width = ViewGroup.LayoutParams.MatchParent;
            layoutParams.Height = ViewGroup.LayoutParams.WrapContent;
            Dialog.Window.Attributes = layoutParams;
        }
    }
}
