// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Diagnostics;

internal abstract class TelemetryProviderBase : IFunctionTelemetryProvider
{
    private static readonly ActivitySource _source
        = new(TraceConstants.FunctionsActivitySource, TraceConstants.FunctionsActivitySourceVersion);

    protected abstract OpenTelemetrySchemaVersion SchemaVersion { get; }

    protected abstract ActivityKind Kind { get; }

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
            TraceConstants.FunctionsInvokeActivityName,
            Kind,
            parent,
            tags: GetTelemetryAttributes(context)!);
    }

    public IEnumerable<KeyValuePair<string, object>> GetTelemetryAttributes(FunctionContext context)
    {
        // Live-logs session
        if (context.TraceContext.Attributes.TryGetValue(TraceConstants.AzFuncLiveLogsSessionIdKey, out var liveId)
            && !string.IsNullOrWhiteSpace(liveId))
        {
            yield return new(TraceConstants.AzFuncLiveLogsSessionIdKey, liveId);
        }

        // Version-specific tags
        foreach (var kv in GetVersionSpecificAttributes(context))
        {
            yield return kv;
        }
    }

    protected abstract IEnumerable<KeyValuePair<string, object>> GetVersionSpecificAttributes(FunctionContext context);
}
