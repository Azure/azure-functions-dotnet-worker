// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

namespace Microsoft.Azure.Functions.Worker
{
    internal class BlobStorageBindingOptions
    {
        public Uri? ServiceUri { get; set; }

        public TokenCredential? Credential { get; set; }

        public BlobClientOptions? BlobClientOptions { get; set; }

        public BlobServiceClient CreateClient()
        {
            return new BlobServiceClient(ServiceUri, Credential, BlobClientOptions);
        }
    }
}