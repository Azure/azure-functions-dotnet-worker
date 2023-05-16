// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Azure.Core;
using Azure.Data.Tables;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tables.Config
{
    internal class TablesBindingOptions
    {
        public string? ConnectionString { get; set; }

        public Uri? ServiceUri { get; set; }

        public TokenCredential? Credential { get; set; }

        public TableClientOptions? TableClientOptions { get; set; }

        internal ConcurrentDictionary<string, TableServiceClient> ClientCache { get; } = new ConcurrentDictionary<string, TableServiceClient>();

        internal virtual TableServiceClient CreateClient()
        {
            if (ConnectionString is null && ServiceUri is null)
            {
                throw new ArgumentNullException(nameof(ConnectionString) + " " + nameof(ServiceUri));
            }
            return !string.IsNullOrEmpty(ConnectionString)
                ? (ClientCache.GetOrAdd(ConnectionString!, (c) => new TableServiceClient(ConnectionString, TableClientOptions))) // Connection string based auth;
                : (ClientCache.GetOrAdd(ServiceUri!.ToString(), (c) => new TableServiceClient(ServiceUri, Credential, TableClientOptions))); // AAD auth
        }
    }
}
