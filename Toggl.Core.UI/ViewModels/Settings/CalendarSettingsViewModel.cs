using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Toggl.Core.Interactors;
using Toggl.Core.Services;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.Navigation;
using Toggl.Core.UI.Services;
using Toggl.Core.UI.ViewModels.Calendar;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Toggl.Storage.Settings;

namespace Toggl.Core.UI.ViewModels.Settings
{
    [Preserve(AllMembers = true)]
    public sealed class CalendarSettingsViewModel : SelectUserCalendarsViewModelBase
    {
        private readonly IPermissionsChecker permissionsChecker;
        private readonly IRxActionFactory rxActionFactory;

        private bool calendarListVisible = false;
        private ISubject<bool> calendarListVisibleSubject = new BehaviorSubject<bool>(false);

        public bool PermissionGranted { get; private set; }
        public IObservable<bool> CalendarListVisible { get; }

        public ViewAction RequestAccess { get; }
        public ViewAction TogglCalendarIntegration { get; }

        public CalendarSettingsViewModel(
            IUserPreferences userPreferences,
            IInteractorFactory interactorFactory,
            INavigationService navigationService,
            IRxActionFactory rxActionFactory,
            IPermissionsChecker permissionsChecker)
            : base(userPreferences, interactorFactory, navigationService, rxActionFactory)
        {
            Ensure.Argument.IsNotNull(permissionsChecker, nameof(permissionsChecker));

            this.permissionsChecker = permissionsChecker;

            RequestAccess = rxActionFactory.FromAction(requestAccess);
            TogglCalendarIntegration = rxActionFactory.FromAsync(togglCalendarIntegration);

            SelectCalendar
                .Elements
                .Subscribe(onCalendarSelected);

            CalendarListVisible = calendarListVisibleSubject.AsObservable().DistinctUntilChanged();
        }

        public override async Task Initialize(bool forceItemSelection)
        {
            PermissionGranted = await permissionsChecker.CalendarPermissionGranted;

            if (!PermissionGranted)
            {
                UserPreferences.SetEnabledCalendars();
            }

            await base.Initialize(forceItemSelection);

            calendarListVisible = PermissionGranted && SelectedCalendarIds.Any();
            calendarListVisibleSubject.OnNext(calendarListVisible);
        }

        public override void CloseWithDefaultResult()
        {
            UserPreferences.SetEnabledCalendars(InitialSelectedCalendarIds.ToArray());
            base.CloseWithDefaultResult();
        }

        protected override void Done()
        {
            if (!calendarListVisible)
                SelectedCalendarIds.Clear();

            UserPreferences.SetEnabledCalendars(SelectedCalendarIds.ToArray());
            base.Done();
        }

        private void requestAccess()
        {
            View.OpenAppSettings();
        }

        private void onCalendarSelected()
        {
            UserPreferences.SetEnabledCalendars(SelectedCalendarIds.ToArray());
        }

        private async Task togglCalendarIntegration()
        {
            if (calendarListVisible)
            {
                calendarListVisible = false;
            }
            else
            {
                var authorized = await permissionsChecker.CalendarPermissionGranted;
                if (!authorized)
                {
                    authorized = await View.RequestCalendarAuthorization();
                    if (!authorized)
                        await Navigate<CalendarPermissionDeniedViewModel, Unit>();

                    calendarListVisible = await permissionsChecker.CalendarPermissionGranted;
                    ReloadCalendars();
                }
                else
                {
                    calendarListVisible = true;
                }
            }
            calendarListVisibleSubject.OnNext(calendarListVisible);
        }
    }
}
