using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

[assembly: WorkerExtensionStartup(typeof(StorageExtensionStartup))]

namespace Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs
{
    public class StorageExtensionStartup : WorkerExtensionStartup
    {
        public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
        {
            applicationBuilder.Services.AddSingleton<BlobStorageConverter>();
        }
    }
}
