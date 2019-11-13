using System;
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

            if (scrollView is NestedScrollView)
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


        public static void UpdatePadding(this View view, int? left = null, int? top = null, int? right = null, int? bottom = null)
        {
            view.SetPadding(
                left.GetValueOrDefault(view.PaddingLeft),
                top.GetValueOrDefault(view.PaddingTop),
                right.GetValueOrDefault(view.PaddingRight),
                bottom.GetValueOrDefault(view.PaddingBottom)
                );
        }

        public static void FitBottomInset(this View view)
        {
            view.DoOnApplyWindowInsets((v, insets, padding) =>
            {
                view.UpdatePadding(bottom: padding.bottom + insets.SystemWindowInsetBottom);
            });
        }

        public static void FitTopInset(this View view)
        {
            view.DoOnApplyWindowInsets((v, insets, padding) =>
            {
                v.UpdatePadding(top: padding.top + insets.SystemWindowInsetTop);
            });
        }

        public static void DoOnApplyWindowInsets(this View view, Action<View, WindowInsets, InitialPadding> insetsHandler)
        {
            // Create a snapshot of the view's padding state
            var initialPadding = view.recordInitialPaddingForView();
            // Set the actual Listener which proxies to insetsHandler, also passing in the original padding state
            view.SetOnApplyWindowInsetsListener(new OnApplyWindowInsetsListener(insetsHandler, initialPadding));
            // request some insets
            view.RequestApplyInsets();
        }

        private class OnApplyWindowInsetsListener : Java.Lang.Object, View.IOnApplyWindowInsetsListener
        {
            private readonly Action<View, WindowInsets, InitialPadding> insetsHandler;
            private readonly InitialPadding initialPadding;

            public OnApplyWindowInsetsListener(Action<View, WindowInsets, InitialPadding> insetsHandler, InitialPadding initialPadding)
            {
                this.insetsHandler = insetsHandler;
                this.initialPadding = initialPadding;
            }

            public WindowInsets OnApplyWindowInsets(View v, WindowInsets insets)
            {
                insetsHandler(v, insets, initialPadding);
                // Always return the insets, so that children can also use them
                return insets;
            }
        }

        private static InitialPadding recordInitialPaddingForView(this View view)
        {
            return new InitialPadding(view.PaddingLeft, view.PaddingTop, view.PaddingRight, view.PaddingBottom);
        }

        public struct InitialPadding
        {
            public readonly int left;
            public readonly int top;
            public readonly int right;
            public readonly int bottom;

            public InitialPadding(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }
        }

        public static void RequestApplyInsetsWhenAttached(this View view)
        {
            if (view.IsAttachedToWindow)
            {
                // We're already attached, just request as normal
                view.RequestApplyInsets();
            }
            else
            {
                // We're not attached to the hierarchy, add a listener to
                // request when we are
                view.AddOnAttachStateChangeListener(new OnAttachStateChangeListener());
            }
        }

        private class OnAttachStateChangeListener : Java.Lang.Object, View.IOnAttachStateChangeListener
        {
            public void OnViewAttachedToWindow(View attachedView)
            {
                attachedView.RemoveOnAttachStateChangeListener(this);
                attachedView.RequestApplyInsets();
            }

            public void OnViewDetachedFromWindow(View detachedView)
            {
            }
        }
    }
}
