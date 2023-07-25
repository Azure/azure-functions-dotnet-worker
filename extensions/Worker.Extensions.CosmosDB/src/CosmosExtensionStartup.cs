// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly: WorkerExtensionStartup(typeof(CosmosExtensionStartup))]

namespace Microsoft.Azure.Functions.Worker
{
    public class CosmosExtensionStartup : WorkerExtensionStartup
    {
        public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
        {
            if (applicationBuilder == null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            applicationBuilder.Services.AddAzureClientsCore(); // Adds AzureComponentFactory

            applicationBuilder.Services.AddOptions<CosmosDBBindingOptions>()
                .Configure<IOptions<WorkerOptions>>((cosmosOptions, workerOptions) => {
                    CosmosSerializer cosmosSerializer = new WorkerCosmosSerializer(workerOptions?.Value?.Serializer);
                    cosmosOptions.Serializer = cosmosSerializer;
                });

            applicationBuilder.Services.AddSingleton<IConfigureOptions<CosmosDBBindingOptions>, CosmosDBBindingOptionsSetup>();
        }
    }
}
