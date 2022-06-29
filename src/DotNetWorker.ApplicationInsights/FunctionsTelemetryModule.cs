using System;
using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights
{
    internal class FunctionsTelemetryModule : ITelemetryModule, IDisposable
    {
        private TelemetryClient _telemetryClient = default!;
        private ActivityListener? _listener;

        public void Initialize(TelemetryConfiguration configuration)
        {
            _telemetryClient = new TelemetryClient(configuration);

            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name.StartsWith("Microsoft.Azure.Functions.Worker"),
                ActivityStarted = activity =>
                {
                    _telemetryClient.StartOperation<DependencyTelemetry>(activity);
                    var dependency = new DependencyTelemetry("Azure.Functions", activity.OperationName, activity.OperationName, null);
                    activity.SetCustomProperty("_depTel", dependency);
                    dependency.Start();
                },
                ActivityStopped = activity =>
                {
                    var dependency = activity.GetCustomProperty("_depTel") as DependencyTelemetry;
                    dependency.Stop();
                    _telemetryClient.TrackDependency(dependency);
                },
                Sample = (ref ActivityCreationOptions<ActivityContext> sampleActivity) => ActivitySamplingResult.AllData
            };

            ActivitySource.AddActivityListener(_listener);
        }

        public void Dispose()
        {
            _telemetryClient?.Flush();
            _listener?.Dispose();
        }
    }
}
