// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions;
using Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


[assembly: WorkerExtensionStartup(typeof(StorageExtensionStartup))]

namespace Microsoft.Azure.Functions.Worker
{
    public class StorageExtensionStartup : WorkerExtensionStartup
    {
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
                RegisterBlobServiceClientsWithConfig(configuration, clientBuilder);
                // RegisterBlobServiceClientsWithMetadata(configuration, clientBuilder);
            });
        }

        private void RegisterBlobServiceClientsWithConfig(IConfiguration configuration, AzureClientFactoryBuilder clientBuilder)
        {
            var allConfigurationEntries = configuration.AsEnumerable();

            foreach (var configEntry in allConfigurationEntries)
            {
                if (configuration.GetWebJobsConnectionStringSection(configEntry.Key) is not { } connectionSection)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(connectionSection.Value) && IsValidStorageConnection(connectionSection))
                {
                    clientBuilder.AddBlobServiceClient(connectionSection.Value).WithName(configEntry.Key);
                }
                else if (connectionSection.TryGetServiceUriForStorageAccounts(Constants.BlobServiceUriSubDomain, out Uri serviceUri))
                {
                    clientBuilder.AddBlobServiceClient(serviceUri).WithName(connectionSection.Key);
                }
            }
        }

        private static bool IsValidStorageConnection(IConfigurationSection section)
        {
            return section.Value?.StartsWith("DefaultEndpointsProtocol=") == true &&
                section.Value.Contains("AccountName=") &&
                section.Value.Contains("AccountKey="); ;
        }

        private void RegisterBlobServiceClientsWithMetadata(IConfiguration configuration, AzureClientFactoryBuilder clientBuilder)
        {
            var connections = new List<string>();

            clientBuilder.AddClient<BlobServiceClient, BlobClientOptions>((_, provider) =>
            {
                string scriptRoot = Environment.GetEnvironmentVariable("FUNCTIONS_APPLICATION_DIRECTORY");
                Console.WriteLine("Azure Functions .NET Worker: scriptroot: " + scriptRoot);
                var metadataProvider = provider.GetService<IFunctionMetadataProvider>();

                if (metadataProvider is null) { throw new Exception("metadataProvider"); }

                var functionMetadataList = metadataProvider.GetFunctionMetadataAsync(scriptRoot).GetAwaiter().GetResult();

                if (functionMetadataList == null || !functionMetadataList.Any())
                {
                    Console.WriteLine("Azure Functions .NET Worker: No function metadata found");
                    throw new InvalidOperationException("No function metadata found");
                }

                connections = GetConnectionNames(functionMetadataList);

                return new BlobServiceClient("UseDevelopmentStorage=true");
            }).WithName("StorageEmulator");

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

                if (connectionSection.TryGetServiceUriForStorageAccounts(Constants.BlobServiceUriSubDomain, out Uri serviceUri))
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
                    Console.WriteLine("Azure Functions .NET Worker: func is null");
                    throw new InvalidOperationException("func is null");
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
    }
}
