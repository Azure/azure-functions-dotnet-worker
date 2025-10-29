// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Diagnostics;

internal sealed class TelemetryProviderV1_17_0 : TelemetryProvider
{
    private static readonly KeyValuePair<string, object> SchemaUrlAttribute =
        new(TraceConstants.OTelAttributes_1_17_0.SchemaUrl, TraceConstants.OTelAttributes_1_17_0.SchemaVersion);

    protected override OpenTelemetrySchemaVersion SchemaVersion
        => OpenTelemetrySchemaVersion.V1_17_0;

    protected override ActivityKind Kind
        => ActivityKind.Server;

    public override IEnumerable<KeyValuePair<string, object>> GetTelemetryAttributes(FunctionContext context)
    {
        foreach (var kv in base.GetTelemetryAttributes(context))
        {
            yield return kv;
        }

        yield return SchemaUrlAttribute;
        yield return new(TraceConstants.InternalKeys.FunctionInvocationId, context.InvocationId);
        yield return new(TraceConstants.InternalKeys.FunctionName, context.FunctionDefinition.Name);
        yield return new(TraceConstants.OTelAttributes_1_17_0.InvocationId, context.InvocationId);
    }
}
