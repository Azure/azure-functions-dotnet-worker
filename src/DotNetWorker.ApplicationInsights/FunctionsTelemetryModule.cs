// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights
{
    internal class FunctionsTelemetryModule : ITelemetryModule, IDisposable
    {
        private const string DependencyTelemetryKey = "_depTel";

        private TelemetryClient _telemetryClient = default!;
        private ActivityListener? _listener;

        public void Initialize(TelemetryConfiguration configuration)
        {
            _telemetryClient = new TelemetryClient(configuration);

            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name.StartsWith(TraceConstants.FunctionsActivitySource),
                ActivityStarted = activity =>
                {
                    var dependency = _telemetryClient.StartOperation<DependencyTelemetry>(activity);
                    activity.SetCustomProperty(DependencyTelemetryKey, dependency);
                },
                ActivityStopped = activity =>
                {
                    // Check for Exceptions events
                    foreach (ActivityEvent activityEvent in activity.Events)
                    {
                        TrackExceptionTelemetryFromActivityEvent(activityEvent, _telemetryClient);
                    }

                    if (activity.GetCustomProperty(DependencyTelemetryKey) is IOperationHolder<DependencyTelemetry> dependencyHolder)
                    {
                        var dependency = dependencyHolder.Telemetry;

                        foreach (var item in activity.Tags)
                        {
                            if (!dependency.Properties.ContainsKey(item.Key))
                            {
                                dependency.Properties[item.Key] = item.Value;
                            }
                        }

                        dependency.Success = activity.Status != ActivityStatusCode.Error;
                        _telemetryClient.StopOperation(dependencyHolder);
                        dependencyHolder.Dispose();
                    }
                },
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
            };

            ActivitySource.AddActivityListener(_listener);
        }

        internal static void TrackExceptionTelemetryFromActivityEvent(ActivityEvent activityEvent, TelemetryClient telemetryClient)
        {
            if (activityEvent.Name == TraceConstants.AttributeExceptionEventName)
            {
                string? exceptionType = null;
                string? exceptionStackTrace = null;
                string? exceptionMessage = null;

                foreach (var tag in activityEvent.Tags)
                {
                    if (tag.Key == TraceConstants.AttributeExceptionType)
                    {
                        exceptionType = tag.Value?.ToString();
                        continue;
                    }
                    if (tag.Key == TraceConstants.AttributeExceptionMessage)
                    {
                        exceptionMessage = tag.Value?.ToString();
                        continue;
                    }
                    if (tag.Key == TraceConstants.AttributeExceptionStacktrace)
                    {
                        exceptionStackTrace = tag.Value?.ToString();
                        continue;
                    }
                }

                ExceptionDetailsInfo edi = new(1, -1, exceptionType, exceptionMessage, exceptionStackTrace != null,
                    exceptionStackTrace, Enumerable.Empty<Microsoft.ApplicationInsights.DataContracts.StackFrame>());

                ExceptionTelemetry et = new(new[] { edi }, SeverityLevel.Error, null, new Dictionary<string, string>() { }, new Dictionary<string, double>() { });

                telemetryClient.TrackException(et);
            }
        }

        public void Dispose()
        {
            _telemetryClient?.Flush();
            _listener?.Dispose();
        }
    }
}
