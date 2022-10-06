// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
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
                    switch (activity.OperationName)
                    {
                        case "Host.Invoke":
                            var request = _telemetryClient.StartOperation<RequestTelemetry>(activity);
                            request.Telemetry.Name = activity.TagObjects.First(p => p.Key == "FunctionName").Value!.ToString();
                            activity.SetCustomProperty("_reqTel", request);
                            break;
                        case "InputBindings":
                            var dependency = _telemetryClient.StartOperation<DependencyTelemetry>(activity);
                            dependency.Telemetry.Type = "Azure.Functions";
                            activity.SetCustomProperty("_depTel", dependency);
                            break;
                        case "Worker.Invoke":
                            dependency = _telemetryClient.StartOperation<DependencyTelemetry>(activity);
                            dependency.Telemetry.Type = "Azure.Functions";
                            activity.SetCustomProperty("_depTel", dependency);
                            break;
                        case "OutputBindings":
                            dependency = _telemetryClient.StartOperation<DependencyTelemetry>(activity);
                            dependency.Telemetry.Type = "Azure.Functions";
                            activity.SetCustomProperty("_depTel", dependency);
                            break;
                        default:
                            break;
                    }
                },
                ActivityStopped = activity =>
                {
                    switch (activity.OperationName)
                    {
                        case "InputBindings":
                            var dependency = activity.GetCustomProperty("_depTel") as IOperationHolder<DependencyTelemetry>;
                            _telemetryClient.StopOperation(dependency);
                            dependency?.Dispose();
                            break;
                        case "Worker.Invoke":
                            dependency = activity.GetCustomProperty("_depTel") as IOperationHolder<DependencyTelemetry>;
                            _telemetryClient.StopOperation(dependency);
                            dependency?.Dispose();
                            break;
                        case "OutputBindings":
                            dependency = activity.GetCustomProperty("_depTel") as IOperationHolder<DependencyTelemetry>;
                            _telemetryClient.StopOperation(dependency);
                            dependency?.Dispose();
                            break;
                        case "Host.Invoke":
                            var request = activity.GetCustomProperty("_reqTel") as IOperationHolder<RequestTelemetry>;
                            _telemetryClient.StopOperation(request);
                            request?.Dispose();
                            break;
                        default:
                            break;
                    }
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
