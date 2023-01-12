// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Azure.Core;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.Functions.Worker
{
    internal class DefaultCosmosDBServiceFactory : ICosmosDBServiceFactory
    {
        private readonly CosmosBindingOptions _options;

        public DefaultCosmosDBServiceFactory(CosmosBindingOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public CosmosClient CreateService(string connectionName, CosmosClientOptions cosmosClientOptions)
        {
            if (string.IsNullOrEmpty(_options.ConnectionString))
            {
                // AAD auth
                return new CosmosClient(_options.AccountEndpoint, _options.Credential, cosmosClientOptions);
            }

            // Connection string based auth
            return new CosmosClient(_options.ConnectionString, cosmosClientOptions);
        }
    }
}
