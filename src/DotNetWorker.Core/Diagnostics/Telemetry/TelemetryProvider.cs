// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Diagnostics;

internal abstract class TelemetryProvider : IFunctionTelemetryProvider
{
    private static readonly ActivitySource _source
        = new(TraceConstants.ActivityAttributes.Name, TraceConstants.ActivityAttributes.Version);

    protected abstract OpenTelemetrySchemaVersion SchemaVersion { get; }

    protected abstract ActivityKind Kind { get; }

    /// <summary>
    /// Creates a telemetry provider based on the provided schema version string.
    /// Returns the default (1.17.0) if no version is provided.
    /// </summary>
    /// <param name="schema"></param>
    /// <returns></returns>
    public static TelemetryProvider Create(string? schema = null)
    {
        if (string.IsNullOrWhiteSpace(schema))
        {
            return Create(OpenTelemetrySchemaVersion.V1_17_0);
        }

        var version = ParseSchemaVersion(schema!);
        return Create(version);
    }

    /// <summary>
    /// Returns a telemetry provider for the specified version.
    /// </summary>
    /// <param name="version"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException "></exception>
    public static TelemetryProvider Create(OpenTelemetrySchemaVersion version)
    {
        return version switch
        {
            OpenTelemetrySchemaVersion.V1_17_0 => new TelemetryProviderV1_17_0(),
            OpenTelemetrySchemaVersion.V1_37_0 => new TelemetryProviderV1_37_0(),
            _ => throw new ArgumentException($"Unsupported OpenTelemetry schema version: {version}")
        };
    }

    /// <summary>
    /// Starts an activity for the function invocation.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public Activity? StartActivityForInvocation(FunctionContext context)
    {
        if (!_source.HasListeners())
        {
            return null;
        }

        ActivityContext.TryParse(
            context.TraceContext.TraceParent,
            context.TraceContext.TraceState,
            out var parent);

        // If there is no parent, we still want to create a new root activity.
        return _source.StartActivity(
            TraceConstants.ActivityAttributes.InvokeActivityName,
            Kind,
            parent,
            tags: GetTagAttributes(context)!);
    }

    /// <summary>
    /// Returns common scope attributes for a schema versions.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public virtual IEnumerable<KeyValuePair<string, object>> GetScopeAttributes(FunctionContext context)
    {
        // Live-logs session
        if (context.TraceContext.Attributes.TryGetValue(TraceConstants.InternalKeys.AzFuncLiveLogsSessionId, out var liveId)
            && !string.IsNullOrWhiteSpace(liveId))
        {
            yield return new(TraceConstants.InternalKeys.AzFuncLiveLogsSessionId, liveId);
        }        
    }

    /// <summary>
    /// Returns common tag attributes for a schema versions.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public abstract IEnumerable<KeyValuePair<string, object>> GetTagAttributes(FunctionContext context);

    /// <summary>
    /// Maps only known version strings to the enum.
    /// If the string is anything else (and was explicitly set), we throw.
    /// </summary>
    private static OpenTelemetrySchemaVersion ParseSchemaVersion(string version)
    {
        return version switch
        {
            "1.17.0" => OpenTelemetrySchemaVersion.V1_17_0,
            "1.37.0" => OpenTelemetrySchemaVersion.V1_37_0,
            _ => throw new ArgumentException(
                     $"Invalid OpenTelemetry schema version '{version}'. ", nameof(version))
        };
    }
}
