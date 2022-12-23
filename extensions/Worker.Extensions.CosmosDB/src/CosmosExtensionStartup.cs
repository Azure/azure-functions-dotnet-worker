using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Extensions.DependencyInjection;

[assembly: WorkerExtensionStartup(typeof(CosmosExtensionStartup))]

namespace Microsoft.Azure.Functions.Worker
{
    public class CosmosExtensionStartup : WorkerExtensionStartup
    {
        public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
        {
            applicationBuilder.Services.Configure<WorkerOptions>((workerOption) =>
            {
                workerOption.InputConverters.RegisterAt<CosmosDBConverter>(0);
            });
        }
    }
}
