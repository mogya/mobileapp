using Android.App;
using Android.App.Job;
using Android.Content;
using Firebase.Messaging;

namespace Toggl.Droid.Services
{
    [Service]
    [IntentFilter(new[] {"com.google.firebase.MESSAGING_EVENT"})]
    public class TogglFirebaseMessagingService : FirebaseMessagingService
    {
        public override void OnMessageReceived(RemoteMessage message)
        {
            var dependencyContainer = AndroidDependencyContainer.Instance;
            var userIsLoggedIn = dependencyContainer.UserAccessManager.CheckIfLoggedIn();
            if (!userIsLoggedIn) return;

            scheduleJob(dependencyContainer);
        }
        
        private void scheduleJob(AndroidDependencyContainer dependencyContainer)
        {
            bool hasPendingJobScheduled = dependencyContainer.KeyValueStorage.GetBool(SyncJobService.HasPendingJobScheduledKey);
            if (hasPendingJobScheduled) return;
            
            dependencyContainer.KeyValueStorage.SetBool(SyncJobService.HasPendingJobScheduledKey, true);
            
            var jobClass = Java.Lang.Class.FromType(typeof(SyncJobService));
            var jobScheduler = (JobScheduler) GetSystemService(JobSchedulerService);
            var serviceName = new ComponentName(this, jobClass);
            var jobInfo = new JobInfo.Builder(SyncJobService.JobId, serviceName)
                .SetImportantWhileForeground(true)
                .SetRequiredNetworkType(NetworkType.Any)
                .Build();

            jobScheduler.Schedule(jobInfo);
        }
    }
}
