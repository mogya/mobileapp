﻿using CoreGraphics;
using Foundation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Core;
using Toggl.Core.Calendar;
using Toggl.Core.Extensions;
using Toggl.Core.UI.Calendar;
using Toggl.Core.UI.Collections;
using Toggl.iOS.Cells.Calendar;
using Toggl.iOS.Views.Calendar;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using UIKit;
using FoundationResources = Toggl.Shared.Resources;

namespace Toggl.iOS.ViewSources
{
    public sealed class CalendarCollectionViewSource : UICollectionViewSource, ICalendarCollectionViewLayoutDataSource
    {
        private static readonly string twelveHoursFormat = Resources.TwelveHoursFormat;
        private static readonly string twentyFourHoursFormat = Resources.TwentyFourHoursFormat;
        private static readonly string editingTwelveHoursFormat = Resources.EditingTwelveHoursFormat;
        private static readonly string editingTwentyFourHoursFormat = Resources.EditingTwentyFourHoursFormat;

        private readonly string itemReuseIdentifier = nameof(CalendarItemView);
        private readonly string hourReuseIdentifier = nameof(HourSupplementaryView);
        private readonly string editingHourReuseIdentifier = nameof(HourSupplementaryView);
        private readonly string currentTimeReuseIdentifier = nameof(CurrentTimeSupplementaryView);

        private readonly ITimeService timeService;
        private readonly ObservableGroupedOrderedCollection<CalendarItem> collection;

        private IList<CalendarItem> calendarItems;
        private IList<CalendarItemLayoutAttributes> layoutAttributes;
        private TimeFormat timeOfDayFormat = TimeFormat.TwelveHoursFormat;
        private DateTime date;
        private NSIndexPath editingItemIndexPath;
        private NSIndexPath runningTimeEntryIndexPath;
        private UICollectionView collectionView;

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();
        private readonly ISubject<CalendarItem> itemTappedSubject = new Subject<CalendarItem>();

        private CalendarCollectionViewLayout layout => collectionView.CollectionViewLayout as CalendarCollectionViewLayout;
        private CalendarLayoutCalculator layoutCalculator;

        public bool IsEditing { get; private set; }

        public IObservable<CalendarItem> ItemTapped => itemTappedSubject.AsObservable();

        public CalendarCollectionViewSource(
            ITimeService timeService,
            UICollectionView collectionView,
            IObservable<TimeFormat> timeOfDayFormat,
            ObservableGroupedOrderedCollection<CalendarItem> collection)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(timeOfDayFormat, nameof(timeOfDayFormat));
            Ensure.Argument.IsNotNull(collection, nameof(collection));
            this.timeService = timeService;
            this.collection = collection;
            this.collectionView = collectionView;

            layoutCalculator = new CalendarLayoutCalculator(timeService);
            calendarItems = new List<CalendarItem>();
            layoutAttributes = new List<CalendarItemLayoutAttributes>();

            registerCells();

            timeOfDayFormat
                .Subscribe(timeOfDayFormatChanged)
                .DisposedBy(disposeBag);

            collection
                .CollectionChange
                .ObserveOn(IosDependencyContainer.Instance.SchedulerProvider.MainScheduler)
                .Subscribe(_ => onCollectionChanges())
                .DisposedBy(disposeBag);

            timeService
                .MidnightObservable
                .Subscribe(dateChanged)
                .DisposedBy(disposeBag);

            timeService
                .CurrentDateTimeObservable
                .DistinctUntilChanged(offset => offset.Minute)
                .ObserveOn(IosDependencyContainer.Instance.SchedulerProvider.MainScheduler)
                .Subscribe(_ => updateLayoutAttributesIfNeeded())
                .DisposedBy(disposeBag);

            onCollectionChanges();
        }

        public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var cell = collectionView.DequeueReusableCell(itemReuseIdentifier, indexPath) as CalendarItemView;
            cell.Layout = layout;
            cell.Item = calendarItems[(int)indexPath.Item];
            cell.IsEditing = IsEditing && indexPath == editingItemIndexPath;
            return cell;
        }

        public override nint GetItemsCount(UICollectionView collectionView, nint section)
            => calendarItems.Count;

        public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var item = calendarItems[indexPath.Row];
            itemTappedSubject.OnNext(item);
        }

        public override UICollectionReusableView GetViewForSupplementaryElement(UICollectionView collectionView, NSString elementKind, NSIndexPath indexPath)
        {
            if (elementKind == CalendarCollectionViewLayout.HourSupplementaryViewKind)
            {
                var reusableView = collectionView.DequeueReusableSupplementaryView(elementKind, hourReuseIdentifier, indexPath) as HourSupplementaryView;
                var hour = date.AddHours((int)indexPath.Item);
                reusableView.SetLabel(hour.ToString(supplementaryHourFormat(), CultureInfo.CurrentCulture));
                return reusableView;
            }
            else if (elementKind == CalendarCollectionViewLayout.EditingHourSupplementaryViewKind)
            {
                var reusableView = collectionView.DequeueReusableSupplementaryView(elementKind, editingHourReuseIdentifier, indexPath) as EditingHourSupplementaryView;
                var attrs = layoutAttributes[(int)editingItemIndexPath.Item];
                var hour = (int)indexPath.Item == 0 ? attrs.StartTime.ToLocalTime() : attrs.EndTime.ToLocalTime();
                reusableView.SetLabel(hour.ToString(editingHourFormat(), CultureInfo.CurrentCulture));
                return reusableView;
            }

            return collectionView.DequeueReusableSupplementaryView(elementKind, currentTimeReuseIdentifier, indexPath);
        }

        public IEnumerable<NSIndexPath> IndexPathsOfCalendarItemsBetweenHours(int minHour, int maxHour)
        {
            if (calendarItems.None())
            {
                return Enumerable.Empty<NSIndexPath>();
            }

            var now = timeService.CurrentDateTime;
            var indices = calendarItems
                .Select((value, index) => new { value, index })
                .Where(t => t.value.StartTime.ToLocalTime().Hour >= minHour || t.value.EndTime(now).ToLocalTime().Hour <= maxHour)
                .Select(t => t.index);

            return indices.Select(index => NSIndexPath.FromItemSection(index, 0));
        }

        public CalendarItemLayoutAttributes LayoutAttributesForItemAtIndexPath(NSIndexPath indexPath)
            => layoutAttributes[(int)indexPath.Item];

        public NSIndexPath IndexPathForEditingItem()
            => editingItemIndexPath;

        public NSIndexPath IndexPathForRunningTimeEntry()
            => runningTimeEntryIndexPath;

        public CalendarItem? CalendarItemAtPoint(CGPoint point)
        {
            var indexPath = collectionView.IndexPathForItemAtPoint(point);
            if (indexPath != null && indexPath.Item < calendarItems.Count)
            {
                return calendarItems[(int)indexPath.Item];
            }
            return null;
        }

        public List<DateTimeOffset> AllItemsStartAndEndTime()
        {
            var startTimes = calendarItems.Select(item => item.StartTime).Distinct();
            var endTimes = calendarItems.Where(item => item.EndTime.HasValue).Select(item => (DateTimeOffset)item.EndTime).Distinct();
            return startTimes.Concat(endTimes).ToList();
        }

        public List<CalendarItemLayoutAttributes> GapsBetweenTimeEntriesOf2HoursOrLess()
        {
            var timeEntries = calendarItems
                .Where(item => item.CalendarId == "")
                .OrderBy(te => te.StartTime)
                .ToList();

            return layoutCalculator.CalculateTwoHoursOrLessGapsLayoutAttributes(timeEntries);
        }

        public void StartEditing()
        {
            IsEditing = true;
            layout.IsEditing = true;
        }

        public void StartEditing(NSIndexPath indexPath)
        {
            IsEditing = true;
            editingItemIndexPath = indexPath;
            layout.IsEditing = true;
            collectionView.ReloadItems(new NSIndexPath[] { indexPath });
        }

        public void StopEditing()
        {
            IsEditing = false;
            layout.IsEditing = false;
            layoutAttributes = calculateLayoutAttributes();
            layout.InvalidateLayoutForVisibleItems();
        }

        public NSIndexPath InsertItemView(DateTimeOffset startTime, TimeSpan duration)
        {
            if (!IsEditing)
                throw new InvalidOperationException("Set IsEditing before calling insert/update/remove");

            editingItemIndexPath = insertCalendarItem(startTime, duration);
            collectionView.InsertItems(new NSIndexPath[] { editingItemIndexPath });
            return editingItemIndexPath;
        }

        public NSIndexPath UpdateItemView(DateTimeOffset startTime, TimeSpan duration)
        {
            if (!IsEditing)
                throw new InvalidOperationException("Set IsEditing before calling insert/update/remove");

            editingItemIndexPath = updateCalendarItem(editingItemIndexPath, startTime, duration);

            updateEditingHours();
            layout.InvalidateLayoutForVisibleItems();

            return editingItemIndexPath;
        }

        public void RemoveItemView()
        {
            if (!IsEditing)
                throw new InvalidOperationException("Set IsEditing before calling insert/update/remove");

            removeCalendarItem(editingItemIndexPath);
            collectionView.DeleteItems(new NSIndexPath[] { editingItemIndexPath });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            disposeBag.Dispose();
        }

        private void timeOfDayFormatChanged(TimeFormat timeFormat)
        {
            timeOfDayFormat = timeFormat;
            collectionView.ReloadData();
        }

        private void onCollectionChanges()
        {
            long? originalId = null;
            if (IsEditing && editingItemIndexPath != null)
            {
                var editingIndex = (int)editingItemIndexPath.Item;
                originalId = calendarItems[editingIndex].TimeEntryId;
            }

            calendarItems = collection.IsEmpty ? new List<CalendarItem>() : collection[0].ToList();
            layoutAttributes = calculateLayoutAttributes();

            var runningIndex = calendarItems.IndexOf(item => item.Duration == null);
            runningTimeEntryIndexPath = runningIndex >= 0 ? NSIndexPath.FromItemSection(runningIndex, 0) : null;

            if (originalId != null)
            {
                var editingIndex = calendarItems.IndexOf(item => item.TimeEntryId == originalId);
                editingItemIndexPath = editingIndex >= 0 ? NSIndexPath.FromItemSection(editingIndex, 0) : null;
            }

            collectionView.ReloadData();
        }

        private void dateChanged(DateTimeOffset dateTimeOffset)
        {
            this.date = dateTimeOffset.ToLocalTime().Date;
        }

        private void updateLayoutAttributesIfNeeded()
        {
            if (runningTimeEntryIndexPath == null)
                return;

            layoutAttributes = calculateLayoutAttributes();
            layout.InvalidateLayout();
        }

        private void registerCells()
        {
            collectionView.RegisterNibForCell(CalendarItemView.Nib, itemReuseIdentifier);

            collectionView.RegisterNibForSupplementaryView(HourSupplementaryView.Nib,
                                                           CalendarCollectionViewLayout.HourSupplementaryViewKind,
                                                           new NSString(hourReuseIdentifier));

            collectionView.RegisterNibForSupplementaryView(EditingHourSupplementaryView.Nib,
                                                           CalendarCollectionViewLayout.EditingHourSupplementaryViewKind,
                                                           new NSString(editingHourReuseIdentifier));

            collectionView.RegisterNibForSupplementaryView(CurrentTimeSupplementaryView.Nib,
                                                           CalendarCollectionViewLayout.CurrentTimeSupplementaryViewKind,
                                                           new NSString(currentTimeReuseIdentifier));
        }

        private IList<CalendarItemLayoutAttributes> calculateLayoutAttributes()
            => layoutCalculator.CalculateLayoutAttributes(calendarItems);

        private NSIndexPath insertCalendarItem(DateTimeOffset startTime, TimeSpan duration)
        {
            var calendarItem = new CalendarItem("", CalendarItemSource.TimeEntry, startTime, duration, FoundationResources.NewTimeEntry, CalendarIconKind.None);

            calendarItems.Add(calendarItem);
            var position = calendarItems.Count - 1;

            layoutAttributes = calculateLayoutAttributes();

            var indexPath = NSIndexPath.FromItemSection(position, 0);
            return indexPath;
        }

        private NSIndexPath updateCalendarItem(NSIndexPath indexPath, DateTimeOffset startTime, TimeSpan? duration)
        {
            var position = (int)indexPath.Item;

            calendarItems[position] = calendarItems[position]
                .WithStartTime(startTime)
                .WithDuration(duration);

            layoutAttributes = calculateLayoutAttributes();

            var updatedIndexPath = NSIndexPath.FromItemSection(position, 0);
            return updatedIndexPath;
        }

        private void removeCalendarItem(NSIndexPath indexPath)
        {
            calendarItems.RemoveAt((int)indexPath.Item);
            layoutAttributes = calculateLayoutAttributes();
        }

        private void updateEditingHours()
        {
            var startEditingHour = collectionView
                .GetSupplementaryView(CalendarCollectionViewLayout.EditingHourSupplementaryViewKind, NSIndexPath.FromItemSection(0, 0)) as EditingHourSupplementaryView;
            var endEditingHour = collectionView
                .GetSupplementaryView(CalendarCollectionViewLayout.EditingHourSupplementaryViewKind, NSIndexPath.FromItemSection(1, 0)) as EditingHourSupplementaryView;

            var attrs = layoutAttributes[(int)editingItemIndexPath.Item];
            if (startEditingHour != null)
            {
                var hour = attrs.StartTime.ToLocalTime();
                startEditingHour.SetLabel(hour.ToString(editingHourFormat(), CultureInfo.CurrentCulture));
            }
            if (endEditingHour != null)
            {
                var hour = attrs.EndTime.ToLocalTime();
                endEditingHour.SetLabel(hour.ToString(editingHourFormat(), CultureInfo.CurrentCulture));
            }
        }

        private string supplementaryHourFormat()
            => timeOfDayFormat.IsTwentyFourHoursFormat ? twentyFourHoursFormat : twelveHoursFormat;

        private string editingHourFormat()
            => timeOfDayFormat.IsTwentyFourHoursFormat ? editingTwentyFourHoursFormat : editingTwelveHoursFormat;
    }
}
