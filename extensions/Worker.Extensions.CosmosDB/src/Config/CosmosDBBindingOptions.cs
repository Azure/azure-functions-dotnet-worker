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

        internal string BuildCacheKey(string connection, string region) => $"{connection}|{region}";

        internal ConcurrentDictionary<string, CosmosClient> ClientCache { get; } = new ConcurrentDictionary<string, CosmosClient>();

        internal virtual CosmosClient GetClient(string preferredLocations = "")
        {
            if (string.IsNullOrEmpty(ConnectionName))
            {
                throw new ArgumentNullException(nameof(ConnectionName));
            }

            string cacheKey = BuildCacheKey(ConnectionName!, preferredLocations);

            CosmosClientOptions cosmosClientOptions = new ()
            {
                ConnectionMode = ConnectionMode.Gateway
            };

            if (!string.IsNullOrEmpty(preferredLocations))
            {
                cosmosClientOptions.ApplicationPreferredRegions = Utilities.ParsePreferredLocations(preferredLocations);
            }

            return ClientCache.GetOrAdd(cacheKey, (c) => CreateService(cosmosClientOptions));
        }

        private CosmosClient CreateService(CosmosClientOptions cosmosClientOptions)
        {
            return string.IsNullOrEmpty(ConnectionString)
                    ? new CosmosClient(AccountEndpoint, Credential, cosmosClientOptions) // AAD auth
                    : new CosmosClient(ConnectionString, cosmosClientOptions); // Connection string based auth
        }
    }
}