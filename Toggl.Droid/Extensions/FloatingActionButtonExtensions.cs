using Android.Graphics.Drawables;
using System;
using Google.Android.Material.FloatingActionButton;

namespace Toggl.Droid.Extensions
{
    public static class FloatingActionButtonExtensions
    {
        public static void SetDrawableImageSafe(this FloatingActionButton button, Drawable drawable)
        {
            var wasHiddenBeforeChange = button.IsOrWillBeHidden;

            button.Hide();
            button.SetImageDrawable(drawable);
            button.Show(new FabVisibilityListener(() =>
            {
                button.ImageMatrix.SetScale(1, 1);
            }));

            if (wasHiddenBeforeChange)
                button.Hide();
        }

        public sealed class FabVisibilityListener : FloatingActionButton.OnVisibilityChangedListener
        {
            private readonly Action onFabHidden;

            public FabVisibilityListener(Action onFabHidden)
            {
                this.onFabHidden = onFabHidden;
            }

            public override void OnHidden(FloatingActionButton fab)
            {
                base.OnHidden(fab);
                onFabHidden();
            }
        }

        public static FabVisibilityListener ToFabVisibilityListener(this Action action)
            => new FabVisibilityListener(action);
    }
}
