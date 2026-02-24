// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Diagnostics;

internal sealed class TelemetryProviderV1_37_0 : TelemetryProvider
{
    private static readonly KeyValuePair<string, object> SchemaUrlAttribute =
        new(TraceConstants.OTelAttributes_1_37_0.SchemaUrl, TraceConstants.OTel_1_37_0_SchemaVersion);
    protected override OpenTelemetrySchemaVersion SchemaVersion
        => OpenTelemetrySchemaVersion.V1_37_0;

    protected override ActivityKind Kind
        => ActivityKind.Internal;

    public override IEnumerable<KeyValuePair<string, object>> GetScopeAttributes(FunctionContext context)
    {
        foreach (var kv in base.GetScopeAttributes(context))
        {
            yield return kv;
        }

        foreach (var kv in GetCommonAttributes(context))
        {
            yield return kv;
        }
    }

    public override IEnumerable<KeyValuePair<string, object>> GetTagAttributes(FunctionContext context)
    {
        foreach (var kv in GetCommonAttributes(context))
        {
            yield return kv;
        }
    }

    protected override string GetActivityName(FunctionContext context)
    {
        return $"{TraceConstants.ActivityAttributes.FunctionActivityName} {context.FunctionDefinition.Name}";
    }

    private IEnumerable<KeyValuePair<string, object>> GetCommonAttributes(FunctionContext context)
    {
        yield return SchemaUrlAttribute;
        yield return new(TraceConstants.OTelAttributes_1_37_0.InvocationId, context.InvocationId);
        yield return new(TraceConstants.OTelAttributes_1_37_0.FunctionName, context.FunctionDefinition.Name);

        if (context.TraceContext.Attributes.TryGetValue(TraceConstants.InternalKeys.HostInstanceId, out var host)
            && !string.IsNullOrEmpty(host))
        {
            yield return new(TraceConstants.OTelAttributes_1_37_0.Instance, host);
        }
    }
}
