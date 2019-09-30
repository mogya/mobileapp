using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Android.Graphics;
using Android.Support.V4.Graphics;
using Android.Text;
using Toggl.Core.Calendar;
using Toggl.Core.UI.Calendar;
using Toggl.Core.UI.Collections;
using Toggl.Droid.Extensions;
using Toggl.Droid.ViewHelpers;
using Toggl.Shared.Extensions;

namespace Toggl.Droid.Views.Calendar
{
    public partial class CalendarDayView
    {
        private readonly Dictionary<string, StaticLayout> textLayouts = new Dictionary<string, StaticLayout>();

        private readonly float calendarItemColorAlpha = 0.25f;
        private float leftMargin;
        private float leftPadding;
        private float rightPadding;
        private float itemSpacing;
        private float minHourHeight;

        private int shortCalendarItemHeight;
        private int regularCalendarItemVerticalPadding;
        private int regularCalendarItemHorizontalPadding;
        private int shortCalendarItemVerticalPadding;
        private int shortCalendarItemHorizontalPadding;
        private int regularCalendarItemFontSize;
        private int shortCalendarItemFontSize;
        
        private readonly RectF eventRect = new RectF();
        private readonly Paint eventsPaint = new Paint(PaintFlags.AntiAlias);
        private readonly Paint textEventsPaint = new Paint(PaintFlags.AntiAlias);
        private readonly Paint editingHoursLabelPaint = new Paint(PaintFlags.AntiAlias);

        private CalendarItemEditInfo itemEditInEditMode = CalendarItemEditInfo.None;
        private readonly CalendarItemStartTimeComparer calendarItemComparer = new CalendarItemStartTimeComparer();
        private int? runningTimeEntryIndex = null;
        private int editingHandlesHorizontalMargins;
        private int editingHandlesRadius;

        public void UpdateItems(ObservableGroupedOrderedCollection<CalendarItem> calendarItems)
        {
            var newItems = calendarItems.IsEmpty
                ? ImmutableList<CalendarItem>.Empty
                : calendarItems[0].ToImmutableList();
            
            updateItemsAndRecalculateEventsAttrs(newItems);
        }
        
        partial void initEventDrawingBackingFields()
        {
            minHourHeight = hourHeight / 4f;
            leftMargin = 68.DpToPixels(Context);
            leftPadding = 4.DpToPixels(Context);
            rightPadding = 4.DpToPixels(Context);
            itemSpacing = 4.DpToPixels(Context);
            availableWidth = Width - leftMargin;

            shortCalendarItemHeight = 18.DpToPixels(Context);
            regularCalendarItemVerticalPadding = 2.DpToPixels(Context);
            regularCalendarItemHorizontalPadding = 4.DpToPixels(Context);
            shortCalendarItemVerticalPadding = 0.5f.DpToPixels(Context);
            shortCalendarItemHorizontalPadding = 2.DpToPixels(Context);
            regularCalendarItemFontSize = 10.DpToPixels(Context);
            shortCalendarItemFontSize = 10.DpToPixels(Context);

            eventsPaint.SetStyle(Paint.Style.FillAndStroke);
            textEventsPaint.TextSize = 12.SpToPixels(Context);
            editingHoursLabelPaint.Color = Context.SafeGetColor(Resource.Color.accent);
            editingHoursLabelPaint.TextAlign = Paint.Align.Right;
            editingHoursLabelPaint.TextSize = 12.SpToPixels(Context);
            editingHandlesHorizontalMargins = 8.DpToPixels(Context);
            editingHandlesRadius = 3.DpToPixels(Context);
        }

        private void updateItemsAndRecalculateEventsAttrs(ImmutableList<CalendarItem> newItems)
        {
            if (availableWidth > 0)
            {
                if (itemEditInEditMode.IsValid && itemEditInEditMode.HasChanged) 
                    newItems = newItems.Sort(calendarItemComparer);
                
                calendarItemLayoutAttributes = calendarLayoutCalculator
                    .CalculateLayoutAttributes(newItems)
                    .Select(calculateCalendarItemRect)
                    .ToImmutableList();
            }

            var runningIndex = newItems.IndexOf(item => item.Duration == null);
            runningTimeEntryIndex = runningIndex >= 0 ? runningIndex : (int?)null;
            calendarItems = newItems;
            updateItemInEditMode();

            PostInvalidate();
        }

        private void updateItemInEditMode()
        {
            var currentItemInEditMode = itemEditInEditMode;
            if (!currentItemInEditMode.IsValid) return;

            var calendarItemsToSearch = calendarItems;
            var calendarItemsAttrsToSearch = calendarItemLayoutAttributes;
            var newCalendarItemInEditModeIndex = calendarItemsToSearch.IndexOf(item => item.Id == currentItemInEditMode.CalendarItem.Id);

            if (newCalendarItemInEditModeIndex < 0)
            {
                itemEditInEditMode = CalendarItemEditInfo.None;
            }
            else
            {
                var newLayoutAttr = calendarItemsAttrsToSearch[newCalendarItemInEditModeIndex];
                itemEditInEditMode = new CalendarItemEditInfo(
                    currentItemInEditMode.CalendarItem,
                    newLayoutAttr,
                    newCalendarItemInEditModeIndex,
                    hourHeight,
                    minHourHeight,
                    timeService.CurrentDateTime);
            }
        }

        partial void processEventsOnLayout(bool changed, int left, int top, int right, int bottom)
        {
            updateItemsAndRecalculateEventsAttrs(calendarItems);
        }

        private CalendarItemRectAttributes calculateCalendarItemRect(CalendarItemLayoutAttributes attrs)
        {
            var totalItemSpacing = (attrs.TotalColumns - 1) * itemSpacing;
            var eventWidth = (availableWidth - leftPadding - rightPadding - totalItemSpacing) / attrs.TotalColumns;
            var left = leftMargin + leftPadding + eventWidth * attrs.ColumnIndex + attrs.ColumnIndex * itemSpacing;
            
            return new CalendarItemRectAttributes(attrs, left, left + eventWidth);
        }
        
        partial void drawCalendarItems(Canvas canvas)
        {
            var itemsToDraw = calendarItems;
            var itemsAttrs = calendarItemLayoutAttributes;
            var currentItemInEditMode = itemEditInEditMode;

            for (var eventIndex = 0; eventIndex < itemsAttrs.Count; eventIndex++)
            {
                var item = itemsToDraw[eventIndex];
                var itemAttr = itemsAttrs[eventIndex];

                if (item.Id == currentItemInEditMode.CalendarItem.Id) continue;

                itemAttr.CalculateRect(hourHeight, minHourHeight, eventRect);
                if (!(eventRect.Bottom > scrollOffset) || !(eventRect.Top - scrollOffset < Height)) continue;
                
                drawCalendarShape(canvas, item, eventRect);
                drawCalendarItemText(canvas, item, eventRect);
            }
            
            drawCalendarItemInEditMode(canvas, currentItemInEditMode);
        }

        private void drawCalendarItemInEditMode(Canvas canvas, CalendarItemEditInfo currentItemInEditMode)
        {
            if (!currentItemInEditMode.IsValid) return;
            
            var calendarItem = currentItemInEditMode.CalendarItem;
            currentItemInEditMode.CalculateRect(eventRect);

            if (!(eventRect.Bottom > scrollOffset) || !(eventRect.Top - scrollOffset < Height)) return;

            drawCalendarShape(canvas, calendarItem, eventRect);
            drawCalendarItemText(canvas, calendarItem, eventRect);
            drawEditingHandles(canvas, currentItemInEditMode);
            canvas.DrawText(startHourLabel, hoursX, eventRect.Top + editingHoursLabelPaint.Descent(), editingHoursLabelPaint);
            canvas.DrawText(endHourLabel, hoursX, eventRect.Bottom + editingHoursLabelPaint.Descent(), editingHoursLabelPaint);
        }

        private void drawEditingHandles(Canvas canvas, CalendarItemEditInfo itemInEditModeToDraw)
        {
            var eventColor = eventsPaint.Color;
            eventsPaint.Color = Color.White;

            canvas.DrawCircle(eventRect.Right - editingHandlesHorizontalMargins, eventRect.Top, editingHandlesRadius, eventsPaint);
            if (itemInEditModeToDraw.OriginalIndex != runningTimeEntryIndex)
                canvas.DrawCircle(eventRect.Left + editingHandlesHorizontalMargins, eventRect.Bottom, editingHandlesRadius, eventsPaint);

            eventsPaint.SetStyle(Paint.Style.Stroke);
            eventsPaint.StrokeWidth = 1.DpToPixels(Context);
            eventsPaint.Color = eventColor;
            
            canvas.DrawCircle(eventRect.Right - editingHandlesHorizontalMargins, eventRect.Top, editingHandlesRadius, eventsPaint);
            if (itemInEditModeToDraw.OriginalIndex != runningTimeEntryIndex)
                canvas.DrawCircle(eventRect.Left + editingHandlesHorizontalMargins, eventRect.Bottom, editingHandlesRadius, eventsPaint);
        }

        private void drawCalendarShape(Canvas canvas, CalendarItem item, RectF calendarItemRect)
        {
            var color = Color.ParseColor(item.Color);
            if (item.Source == CalendarItemSource.Calendar)
            {
                color.A = (byte)(color.A * calendarItemColorAlpha);
                color = new Color(ColorUtils.CompositeColors(color, ColorObject.White));
            }
            eventsPaint.SetStyle(Paint.Style.FillAndStroke);
            eventsPaint.Color = color;
                
            canvas.DrawRoundRect(calendarItemRect, leftPadding / 2, leftPadding / 2, eventsPaint);
        }

        private void drawCalendarItemText(Canvas canvas, CalendarItem calendarItem, RectF calendarItemRect)
        {
            var eventHeight = calendarItemRect.Height();
            var eventWidth = calendarItemRect.Width();
            var fontSize = eventHeight <= shortCalendarItemHeight ? shortCalendarItemFontSize : regularCalendarItemFontSize;
            var textVerticalPadding = eventHeight <= shortCalendarItemHeight ? shortCalendarItemVerticalPadding : regularCalendarItemVerticalPadding;
            textVerticalPadding = (int) Math.Min((eventHeight - fontSize) / 2f, textVerticalPadding);
            var textHorizontalPadding = eventHeight <= shortCalendarItemHeight ? shortCalendarItemHorizontalPadding : regularCalendarItemHorizontalPadding;

            var eventTextLayout = getCalendarItemTextLayout(calendarItem, eventWidth - textHorizontalPadding, fontSize);
            var totalLineHeight = calculateLineHeight(eventHeight, eventTextLayout);

            canvas.Save();
            canvas.Translate(calendarItemRect.Left + textHorizontalPadding, calendarItemRect.Top + textVerticalPadding);
            canvas.ClipRect(0, 0, eventWidth - textHorizontalPadding, totalLineHeight);
            eventTextLayout.Draw(canvas);
            canvas.Restore();
        }

        private static int calculateLineHeight(double eventHeight, StaticLayout eventTextLayout)
        {
            var totalLineHeight = 0;
            for (var i = 0; i < eventTextLayout.LineCount; i++)
            {
                var lineBottom = eventTextLayout.GetLineBottom(i);
                if (lineBottom <= eventHeight)
                {
                    totalLineHeight = lineBottom;
                }
            }

            return totalLineHeight;
        }

        private StaticLayout getCalendarItemTextLayout(CalendarItem item, float eventWidth, int fontSize)
        {
            textLayouts.TryGetValue(item.Id, out var eventTextLayout);
            if (eventTextLayout != null && !(Math.Abs(eventTextLayout.Width - eventWidth) > 0.1) && eventTextLayout.Text == item.Description) 
                return eventTextLayout;
            
            var color = item.Source == CalendarItemSource.Calendar ? Color.ParseColor(item.Color) : Color.White;
            textEventsPaint.Color = color;
            textEventsPaint.TextSize = fontSize;
            
            eventTextLayout = new StaticLayout(item.Description,
                0,
                item.Description.Length,
                new TextPaint(textEventsPaint),
                (int) eventWidth,
                Android.Text.Layout.Alignment.AlignNormal,
                1.0f,
                0.0f,
                true,
                TextUtils.TruncateAt.End,
                (int) eventWidth);
            textLayouts[item.Id] = eventTextLayout;

            return eventTextLayout;
        }
        
        private sealed class CalendarItemStartTimeComparer : Comparer<CalendarItem>
        {
            public override int Compare(CalendarItem x, CalendarItem y) 
                => x.StartTime.LocalDateTime.CompareTo(y.StartTime.LocalDateTime);
        }
    }
}