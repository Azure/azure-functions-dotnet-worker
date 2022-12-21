// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    internal static class Constants
    {
        internal const string BlobExtensionName = "AzureStorageBlobs";
        internal const string Connection = "Connection";
        internal const string ContainerName = "ContainerName";
        internal const string BlobName = "BlobName";

        // Media content types
        internal const string JsonContentType = "application/json";

        internal const string ConnectionAccountName = "AzureWebJobsStorage__accountName";
        internal const string ConnectionBlobUri = "AzureWebJobsStorage__blobServiceUri";
        internal const string ConnectionClientId = "AzureWebJobsStorage__clientId";
        internal const string ConnectionClientSecret = "AzureWebJobsStorage__clientSecret";
        internal const string ConnectionTenantId = "AzureWebJobsStorage__tenantId";
        internal const string ConnectionCredential = "AzureWebJobsStorage__credential";
    }
}
