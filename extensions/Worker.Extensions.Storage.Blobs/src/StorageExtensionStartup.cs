// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

            applicationBuilder.Services.AddAzureClients(clientBuilder =>
            {
                var configuration = applicationBuilder.Services.BuildServiceProvider().GetRequiredService<IConfiguration>();
                clientBuilder.AddBlobServiceClient(configuration);
            });

            applicationBuilder.Services.AddAzureClientsCore(); // Adds AzureComponentFactory

            // applicationBuilder.Services.TryAddSingleton<BlobServiceClientProvider>();

            applicationBuilder.Services.AddOptions<BlobStorageBindingOptions>();
            applicationBuilder.Services.AddSingleton<IConfigureOptions<BlobStorageBindingOptions>, BlobStorageBindingOptionsSetup>();
        }
    }
}
