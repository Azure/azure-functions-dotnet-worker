// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Core;
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

            applicationBuilder.Services.AddAzureClients(clientBuilder =>
            {
                RegisterBlobServiceClients(configuration, clientBuilder);
            });

            // applicationBuilder.Services.AddAzureClientsCore(); // Adds AzureComponentFactory

            // applicationBuilder.Services.TryAddSingleton<BlobServiceClientProvider>();

            // applicationBuilder.Services.AddOptions<BlobStorageBindingOptions>();
            // applicationBuilder.Services.AddSingleton<IConfigureOptions<BlobStorageBindingOptions>, BlobStorageBindingOptionsSetup>();
        }

        private void RegisterBlobServiceClients(IConfiguration configuration, AzureClientFactoryBuilder clientBuilder)
        {
            var allConfigurationEntries = configuration.AsEnumerable();

            foreach (var configEntry in allConfigurationEntries)
            {
                if (configuration.GetWebJobsConnectionStringSection(configEntry.Key) is { } connectionSection)
                {
                    if(!IsBlobStorageConnection(connectionSection))
                    {
                        continue;
                    }

                    var connectionName = GetConnectionName(connectionSection.Path);


                    // TODO: Figure out managed identity
                    // TODO: Figure out how to avoid duplicate client registrations
                    if (!string.IsNullOrWhiteSpace(connectionSection.Value) && connectionSection.Value.Contains("DefaultEndpointsProtocol"))
                    {
                        clientBuilder.AddBlobServiceClient(connectionSection.Value).WithName(connectionName);
                    }
                    else
                    {
                        if (connectionSection.TryGetServiceUriForStorageAccounts(BlobServiceUriSubDomain, out Uri serviceUri)
                            || connectionSection.Value is { } && Uri.TryCreate(connectionSection.Value, UriKind.Absolute, out serviceUri))
                        {
                            clientBuilder.AddBlobServiceClient(serviceUri).WithName(connectionName);
                        }
                    }
                }
            }
        }

        private static bool IsBlobStorageConnection(IConfigurationSection section)
        {
            // Check if the section contains a valid blob connection string format
            bool hasValidBlobConnectionString = section.Value?.StartsWith("DefaultEndpointsProtocol=") == true &&
                section.Value.Contains("AccountName=") &&
                section.Value.Contains("AccountKey=");

            // Check if the section's key is "AzureWebJobsStorage"
            bool isAzureWebJobsStorageKey = section.Key == "AzureWebJobsStorage";

            // Check if the section's path contains "blobServiceUri"
            bool containsBlobServiceUri = section.Path.Contains("blobServiceUri");

            // Check if the section's path contains "accountName"
            bool containsAccountName = section.Path.Contains("accountName");

            // Return true if any of the heuristics match
            return hasValidBlobConnectionString || isAzureWebJobsStorageKey || containsBlobServiceUri || containsAccountName;
        }

        private string GetConnectionName(string path)
        {
            // Check if the path or key contains a colon (:)
            int colonIndex = path.IndexOf(':');
            if (colonIndex >= 0)
            {
                // Extract the name before the colon
                return path.Substring(0, colonIndex);
            }

            // Use the entire path or key as the name
            return path;
        }
    }
}
