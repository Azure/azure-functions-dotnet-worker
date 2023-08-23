// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


[assembly: WorkerExtensionStartup(typeof(StorageExtensionStartup))]

namespace Microsoft.Azure.Functions.Worker
{
    public class StorageExtensionStartup : WorkerExtensionStartup
    {
        private const string BlobServiceUriSubDomain = "blob";

        public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
        {
            if (applicationBuilder == null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            var serviceProvider = applicationBuilder.Services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var metadataProvider = serviceProvider.GetRequiredService<IFunctionMetadataProvider>();

            applicationBuilder.Services.AddAzureClients(clientBuilder =>
            {
                RegisterBlobServiceClients(configuration, clientBuilder, serviceProvider, metadataProvider);
            });

            // applicationBuilder.Services.AddAzureClientsCore(); // Adds AzureComponentFactory

            // applicationBuilder.Services.TryAddSingleton<BlobServiceClientProvider>();

            // applicationBuilder.Services.AddOptions<BlobStorageBindingOptions>();
            // applicationBuilder.Services.AddSingleton<IConfigureOptions<BlobStorageBindingOptions>, BlobStorageBindingOptionsSetup>();
        }

        private void RegisterBlobServiceClients(IConfiguration configuration, AzureClientFactoryBuilder clientBuilder, ServiceProvider services, IFunctionMetadataProvider metadataProvider)
        {
            var azureComponentFactory = services.GetService<AzureComponentFactory>();
            var allConfigurationEntries = configuration.AsEnumerable();

            //function metadata provider, using binding configuration

            foreach (var configEntry in allConfigurationEntries)
            {
                var connectionName = GetConnectionName(configEntry.Key);

                if (configuration.GetWebJobsConnectionStringSection(connectionName) is not { } connectionSection)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(connectionSection.Value) && IsValidStorageConnection(connectionSection))
                {
                    clientBuilder.AddBlobServiceClient(connectionSection.Value).WithName(configEntry.Key);
                }
                else if (connectionSection.TryGetServiceUriForStorageAccounts(BlobServiceUriSubDomain, out Uri serviceUri))
                {
                    var credential = azureComponentFactory?.CreateTokenCredential(connectionSection);
                    clientBuilder.AddBlobServiceClient(serviceUri).WithName(connectionName).WithCredential(credential);
                }
            }
        }

    private static bool IsValidStorageConnection(IConfigurationSection section)
    {
        return section.Value?.StartsWith("DefaultEndpointsProtocol=") == true &&
            section.Value.Contains("AccountName=") &&
            section.Value.Contains("AccountKey="); ;
    }

    private string GetConnectionName(string path)
    {
        int colonIndex = path.IndexOf(':');
        if (colonIndex >= 0)
        {
            return path.Substring(0, colonIndex);
        }

        return path;
    }
}
}

// Without custom token credential creation:
// Unable to find matching constructor while trying to create an instance of BlobServiceClient.
// Expected one of the follow sets of configuration parameters:
// 1. connectionString
// 2. serviceUri
// 3. serviceUri, credential:accountName, credential:accountKey
// 4. serviceUri, credential:signature
// 5. serviceUri

"Storage":
{
    "serviceUri": "https://lilianstorage.blob.core.windows.net/",
}

"storage__blobServiceUri"
"storage__queueServiceUri"


