// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;

namespace FunctionApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //Debugger.Launch();

            var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults(builder =>
            {
                builder.UseAspNetCoreIntegration();
            })
            .ConfigureAspNetCoreIntegration()
            .Build();

            await host.RunAsync();
        }
    }
}
