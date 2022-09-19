// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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
                    var dependency = _telemetryClient.StartOperation<DependencyTelemetry>(activity);
                    dependency.Telemetry.Type = "Azure.Functions";
                    activity.SetCustomProperty("_depTel", dependency);
                },
                ActivityStopped = activity =>
                {
                    var dependency = activity.GetCustomProperty("_depTel") as IOperationHolder<DependencyTelemetry>;
                    _telemetryClient.StopOperation(dependency);
                    dependency?.Dispose();
                },
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
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
