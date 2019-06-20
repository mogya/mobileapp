using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.App.Job;
using Toggl.Core.Extensions;
using static Toggl.Droid.Services.JobServicesConstants;

namespace Toggl.Droid.Services
{
    [Service(
        Name = "com.toggl.giskard.SyncJobService",
        Permission = "android.permission.BIND_JOB_SERVICE",
        Exported = true)]
    public class SyncJobService : JobService
    { 
        public override bool OnStartJob(JobParameters @params)
        {
            Task.Run(() =>
            {
                var dependencyContainer = AndroidDependencyContainer.Instance;
                var keyValueStorage = dependencyContainer.KeyValueStorage;
                if (!dependencyContainer.UserAccessManager.CheckIfLoggedIn())
                {
                    keyValueStorage.SetBool(HasPendingSyncJobServiceScheduledKey, false);
                    JobFinished(@params, false);
                    return;
                }

                var shouldHandlePushNotifications = dependencyContainer.RemoteConfigService.ShouldHandlePushNotifications();
                var shouldRunSync = shouldHandlePushNotifications.Wait();

                if (shouldRunSync)
                {
                    var interactorFactory = dependencyContainer.InteractorFactory;
                    var syncInteractor = getTogglApplication().IsInForeground
                        ? interactorFactory.RunPushNotificationInitiatedSyncInForeground()
                        : interactorFactory.RunPushNotificationInitiatedSyncInBackground();

                    syncInteractor.Execute().Wait();
                }

                keyValueStorage.SetBool(HasPendingSyncJobServiceScheduledKey, false);
                JobFinished(@params, false);
            }).ConfigureAwait(false);

            return true;
        }

        public override bool OnStopJob(JobParameters @params)
        {
            AndroidDependencyContainer.Instance
                .KeyValueStorage
                .SetBool(HasPendingSyncJobServiceScheduledKey, false);

            return false;
        }

        private new TogglApplication Application => base.Application as TogglApplication;

    }
}
