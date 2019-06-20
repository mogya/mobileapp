using System;
using Android.App;
using Android.App.Job;
using Android.Content;
using Firebase.Messaging;
using Toggl.Storage.Settings;
using static Toggl.Droid.Services.JobServicesConstants;

namespace Toggl.Droid.Services
{
    [Service]
    [IntentFilter(new[] {"com.google.firebase.MESSAGING_EVENT"})]
    public class TogglFirebaseMessagingService : FirebaseMessagingService
    {
        private const int jobScheduleExpirationInHours = 1;

        public override void OnMessageReceived(RemoteMessage message)
        {
            var dependencyContainer = AndroidDependencyContainer.Instance;
            var userIsLoggedIn = dependencyContainer.UserAccessManager.CheckIfLoggedIn();
            if (!userIsLoggedIn) return;

            var keyValueStorage = dependencyContainer.KeyValueStorage;
            if (!shouldScheduleSyncJob(keyValueStorage)) return;
            
            keyValueStorage.SetBool(HasPendingSyncJobServiceScheduledKey, true);
            keyValueStorage.SetDateTimeOffset(LastSyncJobScheduledAtKey, DateTimeOffset.Now);

            var jobClass = Java.Lang.Class.FromType(typeof(SyncJobService));
            var jobScheduler = (JobScheduler) GetSystemService(JobSchedulerService);
            var serviceName = new ComponentName(this, jobClass);
            var jobInfo = new JobInfo.Builder(SyncJobServiceJobId, serviceName)
                .SetImportantWhileForeground(true)
                .SetRequiredNetworkType(NetworkType.Any)
                .Build();

            jobScheduler.Schedule(jobInfo);
        }

        private bool shouldScheduleSyncJob(IKeyValueStorage keyValueStorage)
        {
            var now = DateTimeOffset.Now; 
            var hasPendingJobScheduled = keyValueStorage.GetBool(HasPendingSyncJobServiceScheduledKey);
            var lastSyncJobScheduledAt = keyValueStorage.GetDateTimeOffset(LastSyncJobScheduledAtKey).GetValueOrDefault();

            return !hasPendingJobScheduled || now.Subtract(lastSyncJobScheduledAt).TotalHours > jobScheduleExpirationInHours;
        }
    }
}
