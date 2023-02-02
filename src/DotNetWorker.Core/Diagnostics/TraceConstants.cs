// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Diagnostics
{
    internal class TraceConstants
    {
        public const string FunctionsActivitySource = "Microsoft.Azure.Functions.Worker";
        public const string FunctionsActivitySourceVersion = "1.0.0.0";
        public const string FunctionsInvokeActivityName = "Invoke";

        public const string AttributeExceptionEventName = "exception";
        public const string AttributeExceptionType = "exception.type";
        public const string AttributeExceptionMessage = "exception.message";
        public const string AttributeExceptionStacktrace = "exception.stacktrace";
        public const string AttributeExceptionEscaped = "exception.escaped";

        public const string AttributeSchemaUrl = "az.schema_url";
        public static IReadOnlyDictionary<OpenTelemetrySchemaVersion, string> OpenTelemetrySchemaMap =
            new Dictionary<OpenTelemetrySchemaVersion, string>()
            {
                [OpenTelemetrySchemaVersion.v1_17_0] = "https://opentelemetry.io/schemas/1.17.0"
            };

        // from: https://opentelemetry.io/docs/reference/specification/trace/semantic_conventions/faas/
        //       https://opentelemetry.io/docs/reference/specification/resource/semantic_conventions/faas/
        public const string AttributeFaasExecution = "faas.execution";
    }
}
