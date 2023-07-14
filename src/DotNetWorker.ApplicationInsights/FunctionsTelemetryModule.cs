// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights
{
    internal sealed class FunctionsTelemetryModule : ITelemetryModule, IAsyncDisposable
    {
        private const string DependencyTelemetryKey = "_tel";
        private const string DependencyTypeInProc = "InProc";

        private TelemetryClient? _telemetryClient;
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
                    dependency.Telemetry.Type = DependencyTypeInProc; // Required for proper rendering in App Insights.
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

        // We want to translate some tags from the Activity into well-known properties in Functions
        private string MapTagToProperty(string key)
        {
            return key switch
            {
                "faas.execution" => "InvocationId",
                _ => key,
            };
        }
        private static void TrackExceptionTelemetryFromActivityEvent(ActivityEvent activityEvent, TelemetryClient telemetryClient)
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

        public async ValueTask DisposeAsync()
        {
            _listener?.Dispose();

            if (_telemetryClient is not null)
            {
                using CancellationTokenSource cts = new(millisecondsDelay: 5000);
                try
                {
                    await _telemetryClient.FlushAsync(cts.Token);
                }
                catch
                {
                    // Ignore for now; potentially log this in the future.
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}
