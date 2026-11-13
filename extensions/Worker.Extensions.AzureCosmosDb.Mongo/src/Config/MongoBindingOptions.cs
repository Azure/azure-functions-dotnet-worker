// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.Azure.Functions.Worker.Extensions.AzureCosmosDb.Mongo.Auth;
using MongoDB.Driver;

namespace Microsoft.Azure.Functions.Worker.Extensions.AzureCosmosDb.Mongo
{
    /// <summary>
    /// Options used to create MongoDB clients for a named connection.
    /// </summary>
    internal class MongoBindingOptions
    {
        private readonly ConcurrentDictionary<string, IMongoClient> _clientCache = new ConcurrentDictionary<string, IMongoClient>();

        /// <summary>
        /// The name of the connection (app setting key) these options were built from.
        /// </summary>
        public string? ConnectionName { get; set; }

        /// <summary>
        /// The resolved MongoDB connection string.
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Returns a cached <see cref="IMongoClient"/> for the given authentication parameters,
        /// creating one if necessary.
        /// </summary>
        internal IMongoClient GetClient(string? tenantId, string? managedIdentityClientId)
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                throw new InvalidOperationException(
                    $"The connection string for '{ConnectionName}' is not configured. " +
                    "Ensure the app setting exists and contains a valid Azure Cosmos DB for MongoDB connection string.");
            }

            string cacheKey = $"{tenantId}|{managedIdentityClientId}";
            return _clientCache.GetOrAdd(cacheKey, _ => CreateClient(tenantId, managedIdentityClientId));
        }

        private IMongoClient CreateClient(string? tenantId, string? managedIdentityClientId)
        {
            IAuthHandler authHandler = AuthHandlerFactory.Create(tenantId, managedIdentityClientId);
            MongoClientSettings settings = authHandler.ConfigureAuth(ConnectionString!);
            return new MongoClient(settings);
        }
    }
}