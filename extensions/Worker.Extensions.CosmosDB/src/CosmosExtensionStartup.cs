using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

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

            applicationBuilder.Services.AddOptions<CosmosDBBindingOptions>();

            // Adds AzureComponentFactory
            applicationBuilder.Services.AddAzureClientsCore();
            applicationBuilder.Services.AddSingleton<ICosmosDBServiceFactory, DefaultCosmosDBServiceFactory>();

            applicationBuilder.Services.Configure<WorkerOptions>((workerOption) =>
            {
                workerOption.InputConverters.RegisterAt<CosmosDBConverter>(0);
            });
        }
    }
}
