// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Functions.Worker.Extensions;
using Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs;

namespace Microsoft.Azure.Functions.Worker
{
    internal class BlobStorageBindingOptionsSetup : IConfigureNamedOptions<BlobStorageBindingOptions>
    {
        private readonly IConfiguration _configuration;
        private readonly AzureComponentFactory _componentFactory;

        private const string BlobServiceUriSubDomain = "blob";

        public BlobStorageBindingOptionsSetup(IConfiguration configuration, AzureComponentFactory componentFactory)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _componentFactory = componentFactory ?? throw new ArgumentNullException(nameof(componentFactory));
        }

        public void Configure(BlobStorageBindingOptions options)
        {
            Configure(Options.DefaultName, options);
        }

        public void Configure(string? name, BlobStorageBindingOptions options)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = Constants.Storage; // default
            }

            IConfigurationSection connectionSection = _configuration.GetWebJobsConnectionStringSection(name!);

            if (!connectionSection.Exists())
            {
                // Not found
                throw new InvalidOperationException($"Blob storage connection configuration '{name}' does not exist. " +
                                                    "Make sure that it is a defined App Setting.");
            }

            if (!string.IsNullOrWhiteSpace(connectionSection.Value))
            {
                options.ConnectionString = connectionSection.Value;
            }
            else
            {
                if (connectionSection.TryGetServiceUriForStorageAccounts(BlobServiceUriSubDomain, out Uri serviceUri))
                {
                    options.ServiceUri = serviceUri;
                }
            }

            options.BlobClientOptions = (BlobClientOptions)_componentFactory.CreateClientOptions(typeof(BlobClientOptions), null, connectionSection);
            options.Credential = _componentFactory.CreateTokenCredential(connectionSection);
        }
    }
}
