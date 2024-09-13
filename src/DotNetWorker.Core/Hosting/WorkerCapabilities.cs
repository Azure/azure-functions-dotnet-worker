// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    internal static class WorkerCapabilities
    {
        internal const string EnableUserCodeException = "EnableUserCodeException";
        internal const string HandlesInvocationCancelMessage = "HandlesInvocationCancelMessage";
        internal const string HandlesWorkerTerminateMessage = "HandlesWorkerTerminateMessage";
        internal const string HandlesWorkerWarmupMessage = "HandlesWorkerWarmupMessage";
        internal const string IncludeEmptyEntriesInMessagePayload = "IncludeEmptyEntriesInMessagePayload";
        internal const string RawHttpBodyBytes = "RawHttpBodyBytes";
        internal const string RpcHttpBodyOnly = "RpcHttpBodyOnly";
        internal const string RpcHttpTriggerMetadataRemoved = "RpcHttpTriggerMetadataRemoved";
        internal const string TypedDataCollection = "TypedDataCollection";
        internal const string UseNullableValueDictionaryForHttp = "UseNullableValueDictionaryForHttp";
        internal const string WorkerStatus = "WorkerStatus";
    }
}
