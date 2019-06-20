using Android.App;
using Android.App.Job;
using Android.Content;
using Firebase.Messaging;
using static Toggl.Droid.Services.JobServicesConstants;

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

            bool hasPendingJobScheduled = dependencyContainer.KeyValueStorage.GetBool(HasPendingSyncJobServiceScheduledKey);
            if (hasPendingJobScheduled) return;
            
            dependencyContainer.KeyValueStorage.SetBool(HasPendingSyncJobServiceScheduledKey, true);
            
            var jobClass = Java.Lang.Class.FromType(typeof(SyncJobService));
            var jobScheduler = (JobScheduler) GetSystemService(JobSchedulerService);
            var serviceName = new ComponentName(this, jobClass);
            var jobInfo = new JobInfo.Builder(SyncJobServiceJobId, serviceName)
                .SetImportantWhileForeground(true)
                .SetRequiredNetworkType(NetworkType.Any)
                .Build();

            jobScheduler.Schedule(jobInfo);
        } 
    }
}
