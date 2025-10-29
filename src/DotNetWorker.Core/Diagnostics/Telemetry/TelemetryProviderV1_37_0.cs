// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Diagnostics;

internal sealed class TelemetryProviderV1_37_0 : TelemetryProvider
{
    private static readonly KeyValuePair<string, object> SchemaUrlAttribute =
        new(TraceConstants.OTelAttributes_1_37_0.SchemaUrl, TraceConstants.OTelAttributes_1_37_0.SchemaVersion);
    protected override OpenTelemetrySchemaVersion SchemaVersion
        => OpenTelemetrySchemaVersion.V1_37_0;

    protected override ActivityKind Kind
        => ActivityKind.Internal;

    public override IEnumerable<KeyValuePair<string, object>> GetTelemetryAttributes(FunctionContext context)
    {
        foreach (var kv in base.GetTelemetryAttributes(context))
        {
            yield return kv;
        }

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
