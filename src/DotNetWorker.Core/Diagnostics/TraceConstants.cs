// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Diagnostics
{
    internal class TraceConstants
    {
        public static IReadOnlyDictionary<OpenTelemetrySchemaVersion, string> OpenTelemetrySchemaMap =
            new Dictionary<OpenTelemetrySchemaVersion, string>()
            {
                [OpenTelemetrySchemaVersion.V1_17_0] = "https://opentelemetry.io/schemas/1.17.0",
                [OpenTelemetrySchemaVersion.V1_37_0] = "https://opentelemetry.io/schemas/1.37.0"
            };

        public static readonly string FunctionsActivitySourceVersion = typeof(TraceConstants).Assembly.GetName().Version?.ToString() ?? string.Empty;
        public const string FunctionsActivitySource = "Microsoft.Azure.Functions.Worker";        
        public const string FunctionsInvokeActivityName = "Invoke";

        public const string AttributeExceptionEventName = "exception";
        public const string AttributeExceptionType = "exception.type";
        public const string AttributeExceptionMessage = "exception.message";
        public const string AttributeExceptionStacktrace = "exception.stacktrace";
        public const string AttributeExceptionEscaped = "exception.escaped";

        // v1.17.0 attributes
        public const string AttributeFaasExecution = "faas.execution";
        public const string AttributeAzSchemaUrl = "az.schema_url";

        // v1.37.0 attributes
        public const string AttributeFaasInvocationId = "faas.invocation_id";
        public const string AttributeFaasFunctionName = "faas.name";
        public const string AttributeFaasInstance = "faas.instance";
        public const string AttributeSchemaUrl = "schema.url";

        // Internal keys used for mapping
        internal const string FunctionInvocationIdKey = "AzureFunctions_InvocationId";
        internal const string FunctionNameKey = "AzureFunctions_FunctionName";
        internal const string HostInstanceIdKey = "HostInstanceId";
        internal const string AzFuncLiveLogsSessionIdKey = "#AzFuncLiveLogsSessionId";

        // Capability variables
        internal const string WorkerOTelEnabled = "WorkerOpenTelemetryEnabled";
        internal const string WorkerOTelSchemaVersion = "WorkerOpenTelemetrySchemaVersion";
    }
}
