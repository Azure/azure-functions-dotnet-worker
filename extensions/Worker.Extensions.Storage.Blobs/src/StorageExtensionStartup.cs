// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Extensions.DependencyInjection;

[assembly: WorkerExtensionStartup(typeof(StorageExtensionStartup))]

namespace Microsoft.Azure.Functions.Worker
{
    public class StorageExtensionStartup : WorkerExtensionStartup
    {
        public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
        {
            applicationBuilder.Services.Configure<WorkerOptions>((workerOption) =>
            {
                workerOption.InputConverters.RegisterAt<BlobStorageConverter>(0);
            });
        }
    }
}
