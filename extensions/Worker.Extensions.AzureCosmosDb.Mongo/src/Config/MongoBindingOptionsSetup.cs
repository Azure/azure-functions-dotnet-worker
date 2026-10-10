// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.AzureCosmosDb.Mongo
{
    internal class MongoBindingOptionsSetup : IConfigureNamedOptions<MongoBindingOptions>
    {
        private readonly IConfiguration _configuration;

        public MongoBindingOptionsSetup(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void Configure(MongoBindingOptions options) => Configure(Options.DefaultName, options);

        public void Configure(string? connectionName, MongoBindingOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(connectionName))
            {
                connectionName = Constants.DefaultConnectionStringKey;
            }

            IConfigurationSection section = _configuration.GetWebJobsConnectionStringSection(connectionName!);

            if (!section.Exists())
            {
                throw new InvalidOperationException(
                    $"Mongo connection configuration '{connectionName}' does not exist. " +
                    "Make sure that it is a defined App Setting or environment variable.");
            }

            options.ConnectionName = connectionName;
            options.ConnectionString = section.Value;
        }
    }
}