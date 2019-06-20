using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.App;
using Android.App.Job;
using Android.Content;
using Android.Graphics;
using Android.Support.V4.App;
using Android.Util;
using Android.Views;
using Toggl.Core.UI.Services;
using Toggl.Core.UI.Views;
using Toggl.Droid.Helper;
using Toggl.Droid.Services;
using Toggl.Droid.Views;
using Toggl.Shared.Extensions;

namespace Toggl.Droid.Extensions
{
    public static class ActivityExtensions
    {
        private static readonly Color lollipopFallbackStatusBarColor = Color.ParseColor("#2C2C2C");
        private static readonly long[] noNotificationVibrationPattern = { 0L, 0L };
        private static readonly string defaultChannelId = "Toggl";
        private static readonly string defaultChannelName = "Toggl";
        private static readonly string defaultChannelDescription = "Toggl notifications";

        public static void ChangeStatusBarColor(this Activity activity, Color color, bool useDarkIcons = false)
        {
            var window = activity.Window;
            window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            window.ClearFlags(WindowManagerFlags.TranslucentStatus);
            window.SetStatusBarColor(color);

            if (MarshmallowApis.AreNotAvailable)
            {
                if (color == Color.White && useDarkIcons)
                {
                    window.SetStatusBarColor(lollipopFallbackStatusBarColor);
                }
                return;
            }

            window.DecorView.SystemUiVisibility =
                (StatusBarVisibility)(useDarkIcons ? SystemUiFlags.LightStatusBar : SystemUiFlags.Visible);
        }

        public static (int widthPixels, int heightPixels, bool isLargeScreen) GetMetrics(this Activity activity, Context context = null)
        {
            const int largeScreenThreshold = 360;

            context = context ?? activity.ApplicationContext;

            var displayMetrics = new DisplayMetrics();
            activity.WindowManager.DefaultDisplay.GetMetrics(displayMetrics);

            var isLargeScreen = displayMetrics.WidthPixels > largeScreenThreshold.DpToPixels(context);

            return (displayMetrics.WidthPixels, displayMetrics.HeightPixels, isLargeScreen);
        }

        public static JobInfo CreateBackgroundSyncJobInfo(this Context context, long periodicity)
        {
            var javaClass = Java.Lang.Class.FromType(typeof(BackgroundSyncJobSchedulerService));
            var component = new ComponentName(context, javaClass);

            var builder = new JobInfo.Builder(JobServicesConstants.BackgroundSyncJobServiceJobId, component)
                .SetRequiredNetworkType(NetworkType.Any)
                .SetPeriodic(periodicity)
                .SetPersisted(true);

            var jobInfo = builder.Build();
            return jobInfo;
        }

        public static void SetDefaultDialogLayout(this Window window, Activity activity, Context context, int heightDp)
        {
            const int smallScreenWidth = 312;
            const int largeScreenMargins = 72;

            var (widthPixels, heightPixels, isLargeScreen) = activity.GetMetrics(context);

            var width = isLargeScreen
                ? widthPixels - largeScreenMargins.DpToPixels(context)
                : smallScreenWidth.DpToPixels(context);

            var height = heightDp >= 0
                ? heightDp.DpToPixels(context)
                : heightDp;

            window.SetLayout(width, height);
        }

        public static NotificationCompat.Builder CreateNotificationBuilderWithDefaultChannel(this Context context,
            NotificationManager notificationManager)
        {
            if (OreoApis.AreAvailable)
            {
                var channel = new NotificationChannel(defaultChannelId, defaultChannelName, NotificationImportance.Low);
                channel.Description = defaultChannelDescription;
                channel.EnableVibration(false);
                channel.SetVibrationPattern(noNotificationVibrationPattern);
                notificationManager.CreateNotificationChannel(channel);
            }

            var notificationBuilder = new NotificationCompat.Builder(context, defaultChannelId);
            notificationBuilder.SetVibrate(noNotificationVibrationPattern);

            return notificationBuilder;
        }

        public static void CancelAllNotifications(this Context context)
        {
            var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
            notificationManager?.CancelAll();
        }

        public static IObservable<bool> ShowConfirmationDialog(this Activity activity, string title, string message, string confirmButtonText, string dismissButtonText)
        {
            return Observable.Create<bool>(observer =>
            {
                void showDialogIfActivityIsThere()
                {
                    if (activity.IsFinishing)
                    {
                        observer.CompleteWith(false);
                        return;
                    }

                    var builder = new AlertDialog.Builder(activity, Resource.Style.TogglDialog).SetMessage(message)
                        .SetPositiveButton(confirmButtonText, (s, e) => observer.CompleteWith(true));

                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        builder = builder.SetTitle(title);
                    }

                    if (!string.IsNullOrEmpty(dismissButtonText))
                    {
                        builder = builder.SetNegativeButton(dismissButtonText, (s, e) => observer.CompleteWith(false));
                    }

                    var dialog = builder.Create();
                    dialog.CancelEvent += (s, e) => observer.CompleteWith(false);

                    dialog.Show();
                }

                activity.RunOnUiThread(showDialogIfActivityIsThere);

                return Disposable.Empty;
            });
        }
        
        public static IObservable<T> ShowSelectionDialog<T>(this Activity activity, string title, IEnumerable<SelectOption<T>> options, int initialSelectionIndex = 0)
        {
            return Observable.Create<T>(observer =>
            {
                var dialog = new ListSelectionDialog<T>(activity, title, options, initialSelectionIndex, observer.CompleteWith);
                activity.RunOnUiThread(dialog.Show);
                return Disposable.Empty;
            });
        }

        public static IObservable<bool> ShowDestructiveActionConfirmationDialog(this Activity activity, ActionType type, params object[] formatArguments)
        {
            switch (type)
            {
                case ActionType.DiscardNewTimeEntry:
                    return ShowConfirmationDialog(activity, null, Shared.Resources.DiscardThisTimeEntry, Shared.Resources.Discard, Shared.Resources.Cancel);
                case ActionType.DiscardEditingChanges:
                    return ShowConfirmationDialog(activity, null, Shared.Resources.DiscardEditingChanges, Shared.Resources.Discard, Shared.Resources.ContinueEditing);
                case ActionType.DeleteExistingTimeEntry:
                    return ShowConfirmationDialog(activity, null, Shared.Resources.DeleteThisTimeEntry, Shared.Resources.Delete, Shared.Resources.Cancel);
                case ActionType.DeleteMultipleExistingTimeEntries:
                    return ShowConfirmationDialog(activity, null, string.Format(Shared.Resources.DeleteMultipleTimeEntries, formatArguments), Shared.Resources.Delete, Shared.Resources.Cancel);
                case ActionType.DiscardFeedback:
                    return ShowConfirmationDialog(activity, null, Shared.Resources.DiscardMessage, Shared.Resources.Discard, Shared.Resources.ContinueEditing);
            }

            throw new ArgumentOutOfRangeException(nameof(type));
        }
    }
}
