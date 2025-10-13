// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Diagnostics;

internal sealed class TelemetryProviderV1_17_0 : TelemetryProviderBase
{
    protected override OpenTelemetrySchemaVersion SchemaVersion
        => OpenTelemetrySchemaVersion.V1_17_0;

    protected override ActivityKind Kind
        => ActivityKind.Server;

    protected override IEnumerable<KeyValuePair<string, object>> GetVersionSpecificAttributes(FunctionContext context)
    {
        yield return new(TraceConstants.AttributeAzSchemaUrl, TraceConstants.OpenTelemetrySchemaMap[SchemaVersion]);
        yield return new(TraceConstants.FunctionInvocationIdKey, context.InvocationId);
        yield return new(TraceConstants.FunctionNameKey, context.FunctionDefinition.Name);
        yield return new(TraceConstants.AttributeFaasExecution, context.InvocationId);
    }
}
