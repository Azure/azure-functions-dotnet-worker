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
        private readonly IConfiguration _configuration;
        private readonly AzureComponentFactory _componentFactory;

        public DefaultCosmosDBServiceFactory(IConfiguration configuration, AzureComponentFactory componentFactory)
        {
            _configuration = configuration;
            _componentFactory = componentFactory;
        }

        public CosmosClient CreateService(string connectionName, CosmosClientOptions cosmosClientOptions)
        {
            CosmosConnectionInformation cosmosConnectionInformation = ResolveConnectionInformation(connectionName);

            if (cosmosConnectionInformation.UsesConnectionString)
            {
                // Connection string based auth
                return new CosmosClient(cosmosConnectionInformation.ConnectionString, cosmosClientOptions);
            }

            // AAD auth
            return new CosmosClient(cosmosConnectionInformation.AccountEndpoint, cosmosConnectionInformation.Credential, cosmosClientOptions);
        }

        private CosmosConnectionInformation ResolveConnectionInformation(string connection)
        {
            IConfigurationSection connectionSection = _configuration.GetConnectionStringSection(connection);

            if (!connectionSection.Exists())
            {
                // Not found
                throw new InvalidOperationException($"Cosmos DB connection configuration '{connection}' does not exist. " +
                                                    $"Make sure that it is a defined App Setting.");
            }

            if (!string.IsNullOrWhiteSpace(connectionSection.Value))
            {
                return new CosmosConnectionInformation(connectionSection.Value);
            }
            else
            {
                string accountEndpoint = connectionSection[Constants.AccountEndpoint];
                if (string.IsNullOrWhiteSpace(accountEndpoint))
                {
                    // Not found
                    throw new InvalidOperationException($"Connection should have an '{Constants.AccountEndpoint}' property or be a " +
                        $"string representing a connection string.");
                }

                TokenCredential credential = _componentFactory.CreateTokenCredential(connectionSection);
                return new CosmosConnectionInformation(accountEndpoint, credential);
            }
        }

        private class CosmosConnectionInformation
        {
            public CosmosConnectionInformation(string connectionString)
            {
                ConnectionString = connectionString;
                UsesConnectionString = true;
            }

            public CosmosConnectionInformation(string accountEndpoint, TokenCredential tokenCredential)
            {
                AccountEndpoint = accountEndpoint;
                Credential = tokenCredential;
                UsesConnectionString = false;
            }

            public bool UsesConnectionString { get; }

            public string ConnectionString { get; }

            public string AccountEndpoint { get; }

            public TokenCredential Credential { get; }
        }
    }
}
