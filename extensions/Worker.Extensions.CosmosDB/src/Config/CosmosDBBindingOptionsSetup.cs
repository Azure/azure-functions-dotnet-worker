// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Azure.Core;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker
{
    internal class LanguageWorkerOptionsSetup : IConfigureOptions<CosmosDBBindingOptions>
    {
        private readonly IConfiguration _configuration;
        private readonly AzureComponentFactory _componentFactory;

        public LanguageWorkerOptionsSetup(IConfiguration configuration, AzureComponentFactory componentFactory)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _componentFactory = componentFactory ?? throw new ArgumentNullException(nameof(componentFactory));
        }

        public void Configure(CosmosDBBindingOptions options)
        {
            var connection = "this needs to come from the binding";
            _configuration.Bind(connection);
            IConfigurationSection connectionSection = _configuration.GetCosmosConnectionStringSection(connection);

            if (!connectionSection.Exists())
            {
                // Not found
                throw new InvalidOperationException($"Cosmos DB connection configuration '{connection}' does not exist. " +
                                                    "Make sure that it is a defined App Setting.");
            }

            if (!string.IsNullOrWhiteSpace(connectionSection.Value))
            {
                options.ConnectionString = connectionSection.Value;
            }
            else
            {
                options.AccountEndpoint  = connectionSection[Constants.AccountEndpoint];
                if (string.IsNullOrWhiteSpace(options.AccountEndpoint))
                {
                    // Not found
                    throw new InvalidOperationException($"Connection should have an '{Constants.AccountEndpoint}' property or be a " +
                        $"string representing a connection string.");
                }

                options.Credential = _componentFactory.CreateTokenCredential(connectionSection);
            }
        }
    }
}