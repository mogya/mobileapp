using Com.Google.Firebase.Perf;
using Com.Google.Firebase.Perf.Metrics;
using Toggl.Core.Diagnostics;

namespace Toggl.Droid.Services
{
    public sealed class FirebaseStopwatchProviderAndroid : BaseStopwatchProvider
    {
        protected override IStopwatch NativeCreate(MeasuredOperation operation)
            => new AndroidFirebaseStopwatch(operation);

        private class AndroidFirebaseStopwatch : IStopwatch
        {
            private readonly Trace firebaseTrace;

            public MeasuredOperation Operation { get; }

            public AndroidFirebaseStopwatch(MeasuredOperation operation)
            {
                Operation = operation;

#if USE_ANALYTICS
                firebaseTrace = FirebasePerformance.Instance.NewTrace(operation.ToString());
#endif
            }

            public void Start()
            {
#if USE_ANALYTICS
                firebaseTrace.Start();
#endif
            }

            public void Stop()
            {
#if USE_ANALYTICS
                firebaseTrace.Stop();
#endif
            }
        }
    }
}
