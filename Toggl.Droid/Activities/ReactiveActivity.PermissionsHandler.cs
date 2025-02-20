using Android.Content;
using Android.Content.PM;
using Android.Support.V4.Content;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Droid.Extensions;
using Toggl.Droid.Helper;

namespace Toggl.Droid.Activities
{
    public abstract partial class ReactiveActivity<TViewModel> : IPermissionRequesterComponent
    {
        public Subject<bool> CalendarAuthorizationSubject { get; set; }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            this.ProcessRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public Permission CheckPermission(string permission)
            => ContextCompat.CheckSelfPermission(this, permission);

        public void StartActivityIntent(Intent intent)
            => StartActivity(intent);

        public IObservable<bool> RequestCalendarAuthorization(bool force = false)
            => this.ProcessCalendarAuthorizationRequest(force);

        public IObservable<bool> RequestNotificationAuthorization(bool force = false)
            => Observable.Return(true);

        public void OpenAppSettings()
            => this.FireAppSettingsIntent();
    }
}