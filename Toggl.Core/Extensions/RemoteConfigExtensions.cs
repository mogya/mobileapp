using Toggl.Core.Services;
using Toggl.Shared;
using Toggl.Storage.Settings;

namespace Toggl.Core.Extensions
{
    public static class RemoteConfigExtensions
    {
        public static RatingViewConfiguration ReadStoredRatingViewConfiguration(this IKeyValueStorage keyValueStorage)
            => new RatingViewConfiguration(
                keyValueStorage.GetInt(RemoteConfigKeys.RatingViewDelayParameter, 5),
                (keyValueStorage.GetString(RemoteConfigKeys.RatingViewTriggerParameter) ?? string.Empty).ToRatingViewCriterion());

        public static PushNotificationsConfiguration ReadStoredPushNotificationsConfiguration(this IKeyValueStorage keyValueStorage)
            => new PushNotificationsConfiguration(true, true);
    }
}
