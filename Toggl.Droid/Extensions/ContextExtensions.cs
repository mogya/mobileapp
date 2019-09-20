using Android.Content;
using Android.Graphics.Drawables;
using AndroidX.Core.Content;


namespace Toggl.Droid.Extensions
{
    public static class ContextExtensions
    {
        public static VectorDrawable GetVectorDrawable(this Context context, int resourceId)
        {
            return ContextCompat.GetDrawable(context, resourceId) as VectorDrawable;
        }
    }
}
