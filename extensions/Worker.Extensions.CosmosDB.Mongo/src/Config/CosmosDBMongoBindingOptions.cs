// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using MongoDB.Driver;

namespace Microsoft.Azure.Functions.Worker
{
    internal class CosmosDBMongoBindingOptions
    {
        public string? ConnectionName  { get; set; }

        public string? ConnectionString { get; set; }

        internal string BuildCacheKey(string connection, string region) => $"{connection}|{region}";

        internal ConcurrentDictionary<string, IMongoClient> ClientCache { get; } = new ConcurrentDictionary<string, IMongoClient>();

        internal virtual IMongoClient GetClient(string preferredLocations = "")
        {
            if (string.IsNullOrEmpty(ConnectionName))
            {
                throw new ArgumentNullException(nameof(ConnectionName));
            }

            string cacheKey = BuildCacheKey(ConnectionName!, preferredLocations);

            return ClientCache.GetOrAdd(cacheKey, (c) => CreateService());
        }

        private IMongoClient CreateService()
        {
            return new MongoClient(ConnectionString);
        }
    }
}
