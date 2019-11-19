using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace Toggl.Droid.Extensions
{
    public static class LayoutParamsExtensions
    {
        public static ViewGroup.MarginLayoutParams WithMargins(this ViewGroup.MarginLayoutParams self, int? left, int? top, int? right, int? bottom)
        {
            var actualTop = top ?? self.TopMargin;
            var actualLeft = left ?? self.LeftMargin;
            var actualRight = right ?? self.RightMargin;
            var actualBottom = bottom ?? self.BottomMargin;

            switch (self)
            {
                case LinearLayout.LayoutParams linearParams:
                    var newLinearLayoutParams = new LinearLayout.LayoutParams(self);
                    newLinearLayoutParams.SetMargins(actualLeft, actualTop, actualRight, actualBottom);
                    return newLinearLayoutParams;

                case RelativeLayout.LayoutParams relativeParams:
                    var newRelativeLayoutParams = new RelativeLayout.LayoutParams(self);
                    newRelativeLayoutParams.SetMargins(actualLeft, actualTop, actualRight, actualBottom);
                    return newRelativeLayoutParams;

                case FrameLayout.LayoutParams frameParams:
                    var newFrameLayoutParams = new FrameLayout.LayoutParams(self);
                    newFrameLayoutParams.SetMargins(actualLeft, actualTop, actualRight, actualBottom);
                    return newFrameLayoutParams;

                case RecyclerView.LayoutParams recyclerParams:
                    var newRecyclerParams = new FrameLayout.LayoutParams(self);
                    newRecyclerParams.SetMargins(actualLeft, actualTop, actualRight, actualBottom);
                    return newRecyclerParams;
            }

            return null;
        }
    }
}
