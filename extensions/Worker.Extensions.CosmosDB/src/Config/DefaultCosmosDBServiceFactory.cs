// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker
{
    internal class DefaultCosmosDBServiceFactory : ICosmosDBServiceFactory
    {
        private readonly CosmosBindingOptions _options;

        public DefaultCosmosDBServiceFactory(IOptions<CosmosBindingOptions> options)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public CosmosClient CreateService(string connectionName, CosmosClientOptions cosmosClientOptions)
        {
            // How to use `connectionName` with IOptions setup?
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
