using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.Gms.Tasks;
using Firebase.RemoteConfig;
using Toggl.Core.Services;
using Toggl.Shared;
using static Toggl.Core.Services.RemoteConfigKeys;
using GmsTask = Android.Gms.Tasks.Task;

namespace Toggl.Droid.Services
{
    public class RemoteConfigServiceAndroid : IRemoteConfigService
    {
        [Conditional("DEBUG")]
        private void enableDeveloperModeInDebugModel(FirebaseRemoteConfig remoteConfig)
        {
            var settings = new FirebaseRemoteConfigSettings
                .Builder()
                .SetDeveloperModeEnabled(true)
                .Build();

            remoteConfig.SetConfigSettings(settings);
        }

        public IObservable<RatingViewConfiguration> RatingViewConfiguration
            => FetchConfiguration(extractRatingViewConfiguration);

        public IObservable<PushNotificationsConfiguration> PushNotificationsConfiguration
            => FetchConfiguration(extractPushNotificationsConfiguration);
        
        private RatingViewConfiguration extractRatingViewConfiguration(FirebaseRemoteConfig remoteConfig)
            => new RatingViewConfiguration(
                (int)remoteConfig.GetValue(RatingViewDelayParameter).AsLong(),
                remoteConfig.GetString(RatingViewTriggerParameter).ToRatingViewCriterion());

        private PushNotificationsConfiguration extractPushNotificationsConfiguration(FirebaseRemoteConfig remoteConfig)
            => new PushNotificationsConfiguration(
                remoteConfig.GetBoolean(RegisterPushNotificationsTokenWithServerParameter),
                remoteConfig.GetBoolean(HandlePushNotificationsParameter));
            
        private IObservable<TConfiguration> FetchConfiguration<TConfiguration>(Func<FirebaseRemoteConfig, TConfiguration> remoteConfigExtractor) 
            => Observable.Create<TConfiguration>(observer =>
            {
                var remoteConfig = FirebaseRemoteConfig.Instance;

                enableDeveloperModeInDebugModel(remoteConfig);

                remoteConfig.SetDefaults(Resource.Xml.RemoteConfigDefaults);
                
                remoteConfig.Fetch(error =>
                {
                    if (error == null)
                        remoteConfig.ActivateFetched();
                    
                    observer.OnNext(remoteConfigExtractor(remoteConfig));
                    observer.OnCompleted();
                });

                return Disposable.Empty;
            });
    }

    public class RemoteConfigCompletionHandler : Java.Lang.Object, IOnCompleteListener
    {
        private FirebaseRemoteConfig remoteConfig;
        private Action<Exception> action;

        public RemoteConfigCompletionHandler(FirebaseRemoteConfig remoteConfig, Action<Exception> action)
        {
            this.remoteConfig = remoteConfig;
            this.action = action;
        }

        public void OnComplete(GmsTask task)
        {
            action(task.IsSuccessful ? null : task.Exception);
        }
    }

    public static class FirebaseExtensions
    {
        public static void Fetch(this FirebaseRemoteConfig remoteConfig, Action<Exception> action)
        {
            remoteConfig.Fetch().AddOnCompleteListener(new RemoteConfigCompletionHandler(remoteConfig, action));
        }
    }
}
