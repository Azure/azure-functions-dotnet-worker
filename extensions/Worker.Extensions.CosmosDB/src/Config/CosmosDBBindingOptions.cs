// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Azure.Core;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Extensions.CosmosDB;

namespace Microsoft.Azure.Functions.Worker
{
    internal class CosmosDBBindingOptions
    {
        public string? ConnectionName  { get; set; }

        public string? ConnectionString { get; set; }

        public string? AccountEndpoint { get; set; }

        public TokenCredential? Credential { get; set; }

        public CosmosSerializer? Serializer { get; set; }

        public CosmosClientOptions? CosmosClientOptions { get; set; }

        internal string BuildCacheKey(string connection, string region) => $"{connection}|{region}";

        internal ConcurrentDictionary<string, CosmosClient> ClientCache { get; } = new ConcurrentDictionary<string, CosmosClient>();

        internal virtual CosmosClient GetClient(string preferredLocations = "")
        {
            if (string.IsNullOrEmpty(ConnectionName))
            {
                throw new ArgumentNullException(nameof(ConnectionName));
            }

            string cacheKey = BuildCacheKey(ConnectionName!, preferredLocations);

            // Do not override if preferred locations is configured via CosmosClientOptions
            if (!string.IsNullOrEmpty(preferredLocations) && CosmosClientOptions.ApplicationPreferredRegions is null)
            {
                CosmosClientOptions.ApplicationPreferredRegions = Utilities.ParsePreferredLocations(preferredLocations);
            }

            // Do not override if the serializer is configured via CosmosClientOptions
            if (Serializer is not null && CosmosClientOptions.Serializer is null)
            {
                CosmosClientOptions.Serializer = Serializer;
            }

            return ClientCache.GetOrAdd(cacheKey, (c) => CreateService());
        }

        private CosmosClient CreateService()
        {
            return string.IsNullOrEmpty(ConnectionString)
                    ? new CosmosClient(AccountEndpoint, Credential, CosmosClientOptions) // AAD auth
                    : new CosmosClient(ConnectionString, CosmosClientOptions); // Connection string based auth
        }
    }
}