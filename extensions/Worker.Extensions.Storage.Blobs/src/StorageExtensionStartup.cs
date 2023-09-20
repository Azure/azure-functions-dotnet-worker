// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Azure.Identity;
using Azure.Storage.Blobs;
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

            applicationBuilder.Services.AddAzureClients(clientBuilder =>
            {
                RegisterBlobServiceClientsWithMetadata(clientBuilder, configuration);
            });


            // applicationBuilder.Services.AddAzureClientsCore(); // Adds AzureComponentFactory
            // applicationBuilder.Services.TryAddSingleton<BlobServiceClientProvider>();
            // applicationBuilder.Services.AddOptions<BlobStorageBindingOptions>();
            // applicationBuilder.Services.AddSingleton<IConfigureOptions<BlobStorageBindingOptions>, BlobStorageBindingOptionsSetup>();
        }

        private void RegisterBlobServiceClientsWithMetadata(AzureClientFactoryBuilder clientBuilder, IConfiguration configuration)
        {
            // var clients = new Dictionary<string, BlobServiceClient>();
            var connections = new List<string>();

            clientBuilder.AddClient<BlobServiceClient, BlobClientOptions>((options, credential, serviceProvider) =>
            {
                string scriptRoot = Environment.GetEnvironmentVariable("FUNCTIONS_APPLICATION_DIRECTORY");
                var metadataProvider = serviceProvider.GetService<IFunctionMetadataProvider>();
                var functionMetadataList = metadataProvider.GetFunctionMetadataAsync(scriptRoot).GetAwaiter().GetResult();

                if(!functionMetadataList.Any())
                {
                    Console.WriteLine("No function metadata found");
                    throw new Exception("this!!!!!!!");
                }

                connections = GetConnectionNames(functionMetadataList);

                return null!;
            });

            clientBuilder.AddClient<BlobServiceClient, BlobClientOptions>((_, _, provider) =>
            {
                Console.WriteLine("Registering BlobServiceClient with config");
                return new BlobServiceClient("UseDevelopmentStorage=true");
            });

            foreach (string connection in connections)
            {
                IConfigurationSection connectionSection = configuration.GetWebJobsConnectionStringSection(connection!);

                if (!connectionSection.Exists())
                {
                    // Not found
                    throw new InvalidOperationException($"Blob storage connection configuration '{connection}' does not exist. " +
                                                        "Make sure that it is a defined App Setting.");
                }

                if (!string.IsNullOrWhiteSpace(connectionSection.Value))
                {
                    clientBuilder.AddBlobServiceClient(connectionSection.Value).WithName(connection);
                }

                if (connectionSection.TryGetServiceUriForStorageAccounts(BlobServiceUriSubDomain, out Uri serviceUri))
                {
                    clientBuilder.AddBlobServiceClient(serviceUri).WithName(connection);
                }
            }
        }

        private List<string> GetConnectionNames(IEnumerable<IFunctionMetadata> functionMetadataList)
        {
            var connections = new List<string>();
            foreach (var func in functionMetadataList)
            {
                if (func is null)
                {
                    Console.WriteLine("Function metadata is null");
                    continue;
                }

                foreach (var bindingJson in func.RawBindings)
                {
                    var binding = JsonSerializer.Deserialize<JsonElement>(bindingJson);

                    if (binding.TryGetProperty("type", out JsonElement bindingType)
                        && !bindingType.ToString().Equals("blob", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    binding.TryGetProperty("connection", out JsonElement bindingConnection);
                    var connectionName = bindingConnection.ToString();

                    if (string.IsNullOrWhiteSpace(connectionName))
                    {
                        connectionName = "Storage"; // default
                    }

                    connections.Add(connectionName!);
                }
            }
            return connections;
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
