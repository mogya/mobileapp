using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Core.Calendar;
using Toggl.Core.Extensions;
using Toggl.Shared;
using Toggl.Shared.Extensions;

using CalendarItemGroup = System.Collections.Generic.List<(Toggl.Core.Calendar.CalendarItem Item, int Index)>;
using CalendarItemGroups = System.Collections.Generic.List<System.Collections.Generic.List<(Toggl.Core.Calendar.CalendarItem Item, int Index)>>;

namespace Toggl.Core.UI.Calendar
{
    public sealed class CalendarLayoutCalculator
    {
        private static readonly TimeSpan offsetFromNow = TimeSpan.FromMinutes(7);
        private static readonly List<CalendarItemLayoutAttributes> emptyAttributes = new List<CalendarItemLayoutAttributes>();
        private readonly ITimeService timeService;
        private readonly TimeSpan minimumDurationForUIPurposes = TimeSpan.FromMinutes(15);

        public CalendarLayoutCalculator(ITimeService timeService)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            this.timeService = timeService;
        }

        public IList<CalendarItemLayoutAttributes> CalculateLayoutAttributes(IList<CalendarItem> calendarItems)
        {
            if (calendarItems.None())
                return emptyAttributes;

            var attributes = calendarItems
                .Indexed()
                .OrderBy(item => item.Item1.StartTime.LocalDateTime)
                .Aggregate(new CalendarItemGroups(), groupOverlappingItems)
                .SelectMany(calculateLayoutAttributes)
                .OrderBy(item => item.Index)
                .Select(item => item.Item)
                .ToList();
            return attributes;
        }

        /// <summary>
        /// Aggregates the indexed calendar items into buckets. Each bucket contains the sequence of overlapping items.
        /// The items in a bucket don't overlap all with each other, but cannot overlap with items in other buckets.
        /// </summary>
        /// <param name="buckets">The list of agregated items</param>
        /// <param name="indexedItem">The item to put in a bucket</param>
        /// <returns>A list of buckets</returns>
        private CalendarItemGroups groupOverlappingItems(
            CalendarItemGroups buckets,
            (CalendarItem Item, int Index) indexedItem)
        {
            if (buckets.None())
            {
                buckets.Add(new CalendarItemGroup { indexedItem });
                return buckets;
            }

            var now = timeService.CurrentDateTime;
            var group = buckets.Last();
            var maxEndTime = group.Max(i => endTime(i.Item, now));
            if (indexedItem.Item.StartTime.LocalDateTime < maxEndTime)
                group.Add(indexedItem);
            else
                buckets.Add(new CalendarItemGroup { indexedItem });

            return buckets;
        }

        /// <summary>
        /// Calculates the layout attributes for the indexed calendar items in a bucket.
        /// The calculation is done minimizing the number of columns.
        /// </summary>
        /// <param name="bucket"></param>
        /// <returns>An list of indexed calendar attributes</returns>
        private List<(CalendarItemLayoutAttributes Item, int Index)> calculateLayoutAttributes(List<(CalendarItem Item, int Index)> bucket)
        {
            var left = bucket.Where(indexedItem => indexedItem.Item.Source == CalendarItemSource.Calendar).ToList();
            var right = bucket.Where(indexedItem => indexedItem.Item.Source != CalendarItemSource.Calendar).ToList();

            var leftColumns = calculateColumnsForItemsInSource(left);
            var rightColumns = calculateColumnsForItemsInSource(right);

            var groupColumns = leftColumns.Concat(rightColumns).ToList();

            return groupColumns
                .Select((column, columnIndex) =>
                    column.Select(item => (attributesForItem(item.Item, groupColumns.Count, columnIndex), item.Index)))
                .Flatten()
                .ToList();
        }

        private CalendarItemGroups calculateColumnsForItemsInSource(List<(CalendarItem Item, int Index)> bucket)
        {
            var groupColumns = bucket.Aggregate(new CalendarItemGroups(), convertIntoColumns);
            return groupColumns;
        }

        /// <summary>
        /// Aggregates the items into columns, minimizing the number of columns.
        /// This will try to insert an item into the first column without overlapping with other items there,
        /// if that's not possible, will try with the rest of the columns until it's inserted or a new column is required.
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="indexedItem"></param>
        /// <returns></returns>
        private CalendarItemGroups convertIntoColumns(CalendarItemGroups bucket, (CalendarItem Item, int Index) indexedItem)
        {
            if (bucket.None())
            {
                bucket.Add(new CalendarItemGroup { indexedItem });
                return bucket;
            }

            var (column, position) = columnAndPositionToInsertItem(bucket, indexedItem);
            if (column != null)
                column.Insert(position, indexedItem);
            else
                bucket.Add(new CalendarItemGroup { indexedItem });

            return bucket;
        }

        /// <summary>
        /// Returns the column and position in that column to insert the new item.
        /// If the item cannot be inserted, the column is null.
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private (List<(CalendarItem Item, int Index)>, int) columnAndPositionToInsertItem(
            CalendarItemGroups columns,
            (CalendarItem Item, int Index) item)
        {
            var positionToInsert = -1;
            var now = timeService.CurrentDateTime;
            var column = columns.FirstOrDefault(c =>
            {
                var index = c.FindLastIndex(elem => endTime(elem.Item, now) <= item.Item.StartTime.LocalDateTime);
                if (index < 0)
                {
                    return false;
                }
                if (index == c.Count - 1)
                {
                    positionToInsert = c.Count;
                    return true;
                }
                if (c[index + 1].Item.StartTime.LocalDateTime >= endTime(item.Item, now))
                {
                    positionToInsert += 1;
                    return true;
                }
                return false;
            });

            return (column, positionToInsert);
        }

        private DateTimeOffset endTime(CalendarItem calendarItem, DateTimeOffset now)
        {
            var duration = calendarItem.Duration(now, offsetFromNow);
            return duration <= minimumDurationForUIPurposes
                ? calendarItem.StartTime.LocalDateTime + TimeSpan.FromMinutes(15)
                : calendarItem.EndTime(now, offsetFromNow);     
        } 

        private CalendarItemLayoutAttributes attributesForItem(
            CalendarItem calendarItem,
            int totalColumns,
            int columnIndex)
        {
            var now = timeService.CurrentDateTime;
            return new CalendarItemLayoutAttributes(calendarItem.StartTime.LocalDateTime, calendarItem.Duration(now, offsetFromNow), totalColumns, columnIndex);
        }
    }
}
