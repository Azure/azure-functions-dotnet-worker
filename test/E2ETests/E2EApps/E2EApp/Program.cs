// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.E2EApp;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker.E2EApps.CosmosApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(workerApplication => {
/*                    workerApplication.UseMiddleware<MyCustomMiddleware>();
*/                })
                .Build();

            await host.RunAsync();
        }
    }
}
