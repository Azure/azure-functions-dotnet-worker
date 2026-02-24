// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker.Diagnostics;

internal static class TraceConstants
{
    public const string OTel_1_37_0_SchemaVersion = "https://opentelemetry.io/schemas/1.37.0";
    public const string OTel_1_17_0_SchemaVersion = "https://opentelemetry.io/schemas/1.17.0";

    public static class ActivityAttributes
    {
        public static readonly string Version = typeof(ActivityAttributes).Assembly.GetName().Version?.ToString() ?? string.Empty;
        public const string Name = "Microsoft.Azure.Functions.Worker";
        public const string InvokeActivityName = "Invoke";
        public const string FunctionActivityName = "function";
    }

    public static class ExceptionAttributes
    {
        public const string EventName = "exception";
        public const string Type = "exception.type";
        public const string Message = "exception.message";
        public const string Stacktrace = "exception.stacktrace";
        public const string Escaped = "exception.escaped";
    }

    public static class OTelAttributes_1_17_0
    {
        // v1.17.0
        public const string InvocationId = "faas.execution";
        public const string SchemaUrl = "az.schema_url";

        internal static ImmutableArray<string> All = ImmutableArray.Create(InvocationId, SchemaUrl);
    }

    public static class OTelAttributes_1_37_0
    {
        // v1.37.0
        public const string InvocationId = "faas.invocation_id";
        public const string FunctionName = "faas.name";
        public const string Instance = "faas.instance";
        public const string SchemaUrl = "schema.url";

        internal static ImmutableArray<string> All = ImmutableArray.Create(InvocationId, FunctionName, Instance, SchemaUrl);
    }

    public static class KnownAttributes
    {
        /// <summary>
        /// Returns protected attribute names that are set by Azure functions that should not be overriden.
        /// </summary>
        public static readonly ImmutableHashSet<string> All =
            OTelAttributes_1_17_0.All
                .Concat(OTelAttributes_1_37_0.All)
                .Concat(InternalKeys.All)
                .ToImmutableHashSet();
    }

    public static class InternalKeys
    {
        public const string FunctionInvocationId = "AzureFunctions_InvocationId";
        public const string FunctionName = "AzureFunctions_FunctionName";
        public const string HostInstanceId = "HostInstanceId";
        public const string AzFuncLiveLogsSessionId = "#AzFuncLiveLogsSessionId";

        internal static ImmutableArray<string> All = ImmutableArray.Create(FunctionInvocationId, FunctionName, 
            HostInstanceId, AzFuncLiveLogsSessionId);
    }

    public static class FunctionContextKeys
    {
        public const string FunctionContextItemsKey = "AzureFunctions_ActivityTags";
    }

    public static class CapabilityFlags
    {
        public const string WorkerOTelEnabled = "WorkerOpenTelemetryEnabled";
        public const string WorkerOTelSchemaVersion = "WorkerOpenTelemetrySchemaVersion";
    }
}
