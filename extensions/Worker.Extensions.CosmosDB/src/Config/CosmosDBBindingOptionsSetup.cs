// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Extensions;
using Microsoft.Azure.Functions.Worker.Extensions.CosmosDB;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker
{
    internal class CosmosDBBindingOptionsSetup : IConfigureNamedOptions<CosmosDBBindingOptions>
    {
        private readonly IConfiguration _configuration;
        private readonly AzureComponentFactory _componentFactory;
        private readonly IOptionsMonitor<WorkerOptions> _workerOptions;
        private readonly IOptionsMonitor<CosmosDBExtensionOptions> _cosmosExtensionOptions;

        public CosmosDBBindingOptionsSetup(IConfiguration configuration, AzureComponentFactory componentFactory, IOptionsMonitor<WorkerOptions> workerOptions, IOptionsMonitor<CosmosDBExtensionOptions> cosmosExtensionOptions)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _componentFactory = componentFactory ?? throw new ArgumentNullException(nameof(componentFactory));
            _workerOptions = workerOptions ?? throw new ArgumentNullException(nameof(workerOptions));
            _cosmosExtensionOptions = cosmosExtensionOptions ?? throw new ArgumentNullException(nameof(cosmosExtensionOptions));
        }

        public void Configure(CosmosDBBindingOptions options)
        {
            Configure(Options.DefaultName, options);
        }

        public void Configure(string? connectionName, CosmosDBBindingOptions options)
        {
            connectionName ??= Options.DefaultName;
            IConfigurationSection connectionSection = _configuration.GetWebJobsConnectionStringSection(connectionName);

            if (!connectionSection.Exists())
            {
                throw new InvalidOperationException($"Cosmos DB connection configuration '{connectionName}' does not exist. " +
                                                    "Make sure that it is a defined App Setting.");
            }

            options.ConnectionName = connectionName;
            if (!string.IsNullOrWhiteSpace(connectionSection.Value))
            {
                options.ConnectionString = connectionSection.Value;
            }
            else
            {
                options.AccountEndpoint = connectionSection[Constants.AccountEndpoint];
                if (string.IsNullOrWhiteSpace(options.AccountEndpoint))
                {
                    throw new InvalidOperationException($"Connection should have an '{Constants.AccountEndpoint}' property or be a " +
                        $"string representing a connection string.");
                }

                options.Credential = _componentFactory.CreateTokenCredential(connectionSection);
            }

            options.CosmosExtensionOptions = _cosmosExtensionOptions.CurrentValue;
        }
    }
}
