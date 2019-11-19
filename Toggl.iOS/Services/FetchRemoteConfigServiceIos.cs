using System;
using Firebase.RemoteConfig;
using Toggl.Core.Services;
using Toggl.Shared;
using static Toggl.Core.Services.RemoteConfigKeys;

namespace Toggl.iOS.Services
{
    public class FetchRemoteConfigServiceIos : IFetchRemoteConfigService
    {
        private const string remoteConfigDefaultsFileName = "RemoteConfigDefaults";
        
        public FetchRemoteConfigServiceIos()
        {
#if !DEBUG
            RemoteConfig.SharedInstance.SetDefaults(plistFileName: remoteConfigDefaultsFileName);
#endif
        }

        public void FetchRemoteConfigData(Action onFetchSucceeded, Action<Exception> onFetchFailed)
        {
            var remoteConfig = RemoteConfig.SharedInstance;
            remoteConfig.Fetch((status, error) =>
            {
                if (error != null)
                    onFetchFailed(new Exception(error.Description));
                else
                {
                    remoteConfig.ActivateFetched();
                    onFetchSucceeded();
                }
            });
        }

        public RatingViewConfiguration ExtractRatingViewConfigurationFromRemoteConfig()
        {
            var remoteConfig = RemoteConfig.SharedInstance;
            return new RatingViewConfiguration(
                remoteConfig[RatingViewDelayParameter].NumberValue.Int32Value,
                remoteConfig[RatingViewTriggerParameter].StringValue.ToRatingViewCriterion());
        }

        public PushNotificationsConfiguration ExtractPushNotificationsConfigurationFromRemoteConfig()
        {
            var remoteConfig = RemoteConfig.SharedInstance;
            return new PushNotificationsConfiguration(
                remoteConfig[RegisterPushNotificationsTokenWithServerParameter].BoolValue,
                remoteConfig[HandlePushNotificationsParameter].BoolValue);
        }
    }
}
