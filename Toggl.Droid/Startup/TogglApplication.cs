using System;
using Android.App;
using Android.Arch.Lifecycle;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Java.Interop;
using Toggl.Core;
using Toggl.Core.UI;
using Toggl.Droid.BroadcastReceivers;

namespace Toggl.Droid
{
    [Application(AllowBackup = false)]
    public class TogglApplication : Application, ILifecycleObserver
    {
        public TimezoneChangedBroadcastReceiver TimezoneChangedBroadcastReceiver { get; set; }

        public bool IsInForeground { get; private set; } = false;
        
        public TogglApplication(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
            ProcessLifecycleOwner.Get().Lifecycle.AddObserver(this);
            Firebase.FirebaseApp.InitializeApp(this);

            AndroidDependencyContainer.EnsureInitialized(Context);
            var app = new AppStart(AndroidDependencyContainer.Instance);
            var accessLevel = app.GetAccessLevel();
            app.SetupBackgroundSync();
            app.SetFirstOpened();
            if (accessLevel == AccessLevel.TokenRevoked || accessLevel == AccessLevel.LoggedIn)
            {
                AndroidDependencyContainer.Instance
                    .UserAccessManager
                    .LoginWithSavedCredentials();
            }
#if USE_APPCENTER
            Microsoft.AppCenter.AppCenter.Start(
                "{TOGGL_APP_CENTER_ID_DROID}",
                typeof(Microsoft.AppCenter.Crashes.Crashes),
                typeof(Microsoft.AppCenter.Analytics.Analytics));
#endif
        }

        [Export]
        [Lifecycle.Event.OnStart]
        public void OnEnterForeground()
        {
            IsInForeground = true;
            var backgroundService = AndroidDependencyContainer.Instance?.BackgroundService;
            backgroundService?.EnterForeground();
        }

        [Export]
        [Lifecycle.Event.OnStop]
        public void OnEnterBackground()
        {
            IsInForeground = false;
            var backgroundService = AndroidDependencyContainer.Instance?.BackgroundService;
            backgroundService?.EnterBackground();
        }

        public override void OnTrimMemory(TrimMemory level)
        {
            base.OnTrimMemory(level);
            switch (level)
            {
                case TrimMemory.RunningCritical:
                case TrimMemory.RunningLow:
                    AndroidDependencyContainer.Instance
                        ?.AnalyticsService
                        ?.ReceivedLowMemoryWarning
                        ?.Track(Platform.Giskard);
                    break;
            }
        }
    }
}
