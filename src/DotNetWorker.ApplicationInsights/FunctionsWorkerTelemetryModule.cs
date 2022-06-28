﻿using System;
using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace DotNetWorker.ApplicationInsights
{
    internal class FunctionsWorkerTelemetryModule : ITelemetryModule, IDisposable
    {
        private ActivityListener? _listener;

        public void Initialize(TelemetryConfiguration configuration)
        {
            var telemetryClient = new TelemetryClient(configuration);

            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name.StartsWith("Microsoft.Azure.Functions.Worker"),
                ActivityStarted = activity =>
                {
                    telemetryClient.StartOperation<DependencyTelemetry>(activity);
                    var dependency = new DependencyTelemetry("Azure.Functions", activity.OperationName, activity.OperationName, null);
                    activity.AddTag("_depTel", dependency);
                    dependency.Start();
                },
                ActivityStopped = activity =>
                {
                    var dependency = activity.GetTagItem("_depTel") as DependencyTelemetry;
                    dependency.Stop();
                    telemetryClient.TrackDependency(dependency);
                },
                Sample = (ref ActivityCreationOptions<ActivityContext> sampleActivity) => ActivitySamplingResult.AllData
            };

            ActivitySource.AddActivityListener(_listener);
        }

        public void Dispose()
        {
            _listener?.Dispose();
        }
    }
}
