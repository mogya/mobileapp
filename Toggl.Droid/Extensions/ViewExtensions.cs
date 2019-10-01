using Android.Animation;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.Design.Widget;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Views.InputMethods;
using System.Collections.Generic;
using System.Linq;
using Toggl.Shared;
using Color = Toggl.Shared.Color;
using AndroidColor = Android.Graphics.Color;
using Android.App;
using Android.Util;
using Toggl.Droid.Helper;
using Toggl.Droid.Views.Calendar;

namespace Toggl.Droid.Extensions
{
    public static class ViewExtensions
    {
        public static IEnumerable<View> GetChildren(this ViewGroup view)
        {
            for (int i = 0; i < view.ChildCount; i++)
                yield return view.GetChildAt(i);
        }

        public static IEnumerable<T> GetChildren<T>(this ViewGroup view)
            => view.GetChildren().OfType<T>();

        public static void SetFocus(this View view)
        {
            var service = (InputMethodManager)view.Context.GetSystemService(Context.InputMethodService);

            view.Post(() =>
            {
                view.RequestFocus();
                service.ShowSoftInput(view, ShowFlags.Forced);
            });
        }

        public static void RemoveFocus(this View view)
        {
            var service = (InputMethodManager)view.Context.GetSystemService(Context.InputMethodService);

            view.Post(() =>
            {
                view.ClearFocus();
                service.HideSoftInputFromWindow(view.WindowToken, 0);
            });
        }

        public static void AttachMaterialScrollBehaviour(this View scrollView, AppBarLayout appBarLayout)
        {
            if (MarshmallowApis.AreNotAvailable)
                return;

            scrollView.SetOnScrollChangeListener(new MaterialScrollBehaviorListener(appBarLayout));

            if (scrollView is NestedScrollView || scrollView is CalendarDayView)
            {
                appBarLayout.Post(() => appBarLayout.Elevation = 0);
            }
        }

        private class MaterialScrollBehaviorListener : Java.Lang.Object, View.IOnScrollChangeListener
        {
            private readonly AppBarLayout appBarLayout;
            private const int defaultToolbarElevationInDPs = 4;

            public MaterialScrollBehaviorListener(AppBarLayout appBarLayout)
            {
                this.appBarLayout = appBarLayout;
            }

            public void OnScrollChange(View v, int scrollX, int scrollY, int oldScrollX, int oldScrollY)
            {
                var targetElevation = v.CanScrollVertically(-1) ?  defaultToolbarElevationInDPs.DpToPixels(appBarLayout.Context) : 0f;
                appBarLayout.Elevation = targetElevation;
            }
        }
    }
}
