// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Azure.Core;
using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Extensions.CosmosDB;

namespace Microsoft.Azure.Functions.Worker
{
     /// <summary>
    /// Internal options for configuring the CosmosDB binding.
    /// This class is used internally by the Azure Functions runtime to manage the CosmosDB connection and clients.
    /// It is not intended to be used directly in user code.
    /// Any public configuration options should be set on the <see cref="CosmosDBExtensionOptions"/> class, which is publicly accessible.
    /// </summary>
    internal class CosmosDBBindingOptions
    {
        private static readonly JsonObjectSerializer DefaultSerializer = new(new() { PropertyNameCaseInsensitive = true });

        public string? ConnectionName { get; set; }

        public string? ConnectionString { get; set; }

        public string? AccountEndpoint { get; set; }

        public TokenCredential? Credential { get; set; }

        public CosmosDBExtensionOptions? CosmosExtensionOptions { get; set; }

        public ObjectSerializer Serializer => CosmosExtensionOptions?.Serializer ?? DefaultSerializer;

        internal string BuildCacheKey(string connection, string region) => $"{connection}|{region}";

        internal ConcurrentDictionary<string, CosmosClient> ClientCache { get; } = new ConcurrentDictionary<string, CosmosClient>();

        internal virtual CosmosClient GetClient(string preferredLocations = "")
        {
            if (string.IsNullOrEmpty(ConnectionName))
            {
                throw new ArgumentNullException(nameof(ConnectionName));
            }

            if (CosmosExtensionOptions is null)
            {
                CosmosExtensionOptions = new CosmosDBExtensionOptions();
            }

            string cacheKey = BuildCacheKey(ConnectionName!, preferredLocations);

            // Do not override if preferred locations is configured via CosmosClientOptions
            if (!string.IsNullOrEmpty(preferredLocations) && CosmosExtensionOptions.ClientOptions.ApplicationPreferredRegions is null)
            {
                // Clone to avoid modifying the original options
                CosmosClientOptions cosmosClientOptions = CosmosExtensionOptions.ClientOptions.MemberwiseClone<CosmosClientOptions>();
                cosmosClientOptions.ApplicationPreferredRegions = Utilities.ParsePreferredLocations(preferredLocations);
                return ClientCache.GetOrAdd(cacheKey, (c) => CreateService(cosmosClientOptions));
            }

            return ClientCache.GetOrAdd(cacheKey, (c) => CreateService(CosmosExtensionOptions.ClientOptions));
        }

        private CosmosClient CreateService(CosmosClientOptions cosmosClientOptions)
        {
            return string.IsNullOrEmpty(ConnectionString)
                    ? new CosmosClient(AccountEndpoint, Credential, cosmosClientOptions) // AAD auth
                    : new CosmosClient(ConnectionString, cosmosClientOptions); // Connection string based auth
        }
    }
}
