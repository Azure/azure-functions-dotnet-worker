// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text.Json;
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
            // var metadataProvider = serviceProvider.GetRequiredService<IFunctionMetadataProvider>(); // doesn't work

            applicationBuilder.Services.AddAzureClients(clientBuilder =>
            {
                RegisterBlobServiceClientsWithConfig(configuration, clientBuilder, serviceProvider);
            });

            // applicationBuilder.Services.AddAzureClientsCore(); // Adds AzureComponentFactory
            // applicationBuilder.Services.TryAddSingleton<BlobServiceClientProvider>();
            // applicationBuilder.Services.AddOptions<BlobStorageBindingOptions>();
            // applicationBuilder.Services.AddSingleton<IConfigureOptions<BlobStorageBindingOptions>, BlobStorageBindingOptionsSetup>();
        }

        private void RegisterBlobServiceClientsWithMetadata(AzureClientFactoryBuilder clientBuilder, ServiceProvider services, IFunctionMetadataProvider metadataProvider, IConfiguration configuration)
        {
            var azureComponentFactory = services.GetService<AzureComponentFactory>();

            string scriptRoot = Environment.GetEnvironmentVariable("FUNCTIONS_APPLICATION_DIRECTORY");
            var functionMetadataList = metadataProvider.GetFunctionMetadataAsync(scriptRoot).GetAwaiter().GetResult();
            foreach (var func in functionMetadataList)
            {
                if (func is null)
                {
                    continue;
                }

                foreach (var bindingJson in func.RawBindings)
                {
                    // for each binding where binding type == blob

                    var binding = JsonSerializer.Deserialize<JsonElement>(bindingJson);
                    binding.TryGetProperty("connection", out JsonElement conn);
                    var connectionName = conn.ToString();

                    if (configuration.GetWebJobsConnectionStringSection(connectionName) is not { } connectionSection)
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(connectionSection.Value) && IsValidStorageConnection(connectionSection))
                    {
                        clientBuilder.AddBlobServiceClient(connectionSection.Value).WithName(connectionName);
                    }
                    else if (connectionSection.TryGetServiceUriForStorageAccounts(BlobServiceUriSubDomain, out Uri serviceUri))
                    {
                        var credential = azureComponentFactory?.CreateTokenCredential(connectionSection);
                        clientBuilder.AddBlobServiceClient(serviceUri).WithName(connectionName).WithCredential(credential);
                    }
                }
            }
        }

        private void RegisterBlobServiceClientsWithConfig(IConfiguration configuration, AzureClientFactoryBuilder clientBuilder, ServiceProvider services)
        {
            var azureComponentFactory = services.GetService<AzureComponentFactory>();
            var allConfigurationEntries = configuration.AsEnumerable();

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
