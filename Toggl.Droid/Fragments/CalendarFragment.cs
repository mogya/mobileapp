using Android.OS;
using Android.Util;
using Android.Views;
using System;
using System.Reactive.Linq;
using Toggl.Core.UI.ViewModels.Calendar;
using Toggl.Droid.Extensions;
using Toggl.Droid.Presentation;
using Toggl.Shared.Extensions;

namespace Toggl.Droid.Fragments
{
    public partial class CalendarFragment : ReactiveTabFragment<CalendarViewModel>, IScrollableToStart
    {
        private readonly TimeSpan defaultTimeSpanSize = TimeSpan.FromMinutes(30);
        private CalendarDayViewModel calendarDayViewModel;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.CalendarFragment, container, false);
            InitializeViews(view);
            calendarDayView.AttachMaterialScrollBehaviour(appBarLayout);
            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            var timeService = AndroidDependencyContainer.Instance.TimeService;
            var schedulerProvider = AndroidDependencyContainer.Instance.SchedulerProvider;
            var today = timeService.CurrentDateTime.LocalDateTime;

            calendarDayViewModel = ViewModel.DayViewModelFor(today);
            
            var displayMetrics = new DisplayMetrics();
            Activity.WindowManager.DefaultDisplay.GetMetrics(displayMetrics);
            
            timeService
                .CurrentDateTimeObservable
                .DistinctUntilChanged(offset => offset.Day)
                .ObserveOn(schedulerProvider.MainScheduler)
                .Subscribe(configureHeaderDate)
                .DisposedBy(DisposeBag);

            calendarDayView.UpdateItems(calendarDayViewModel.CalendarItems);
            
            calendarDayViewModel.CalendarItems.CollectionChange
                .Subscribe(_ => calendarDayView.UpdateItems(calendarDayViewModel.CalendarItems))
                .DisposedBy(DisposeBag);

            calendarDayView.CalendarItemTappedObservable
                .Subscribe(calendarDayViewModel.OnItemTapped.Inputs)
                .DisposedBy(DisposeBag);

            calendarDayView.EmptySpansTouchedObservable
                .Select(emptySpan => (emptySpan, defaultTimeSpanSize))
                .Subscribe(calendarDayViewModel.OnDurationSelected.Inputs)
                .DisposedBy(DisposeBag);

            calendarDayView.EditCalendarItem
                .Subscribe(calendarDayViewModel.OnUpdateTimeEntry.Inputs)
                .DisposedBy(DisposeBag);
        }

        public void ScrollToStart()
        {
            calendarDayView?.ScrollToCurrentHour(true);
        }

        public override void OnResume()
        {
            base.OnResume();
            calendarDayView?.ScrollToCurrentHour();
        }

        private void configureHeaderDate(DateTimeOffset offset)
        {
            var dayText = offset.ToString(Shared.Resources.CalendarToolbarDateFormat);
            headerDateTextView.Text = dayText;
        }
    }
}
