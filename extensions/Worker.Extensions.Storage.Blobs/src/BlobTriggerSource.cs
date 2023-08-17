// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Specifies the blob trigger source used to detect blob changes.
    /// </summary>
    public enum BlobTriggerSource
    {
        /// <summary>
        /// Polling works as a hybrid between inspecting logs and running periodic container scans. Blobs are scanned in groups of 10,000 at a time with a continuation token used between intervals.
        /// <see href="https://docs.microsoft.com/en-us/rest/api/storageservices/storage-analytics-log-format">Storage Analytics logs</see>
        /// </summary>
        LogsAndContainerScan,
        /// <summary>
        /// Uses Event Grid as the source of change notifications.
        /// <see href="https://docs.microsoft.com/en-us/azure/event-grid/event-schema-blob-storage">Azure Blob Storage as an Event Grid source</see>
        /// </summary>
        EventGrid
    }
}
