// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FunctionApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
// #if DEBUG
//          Debugger.Launch();
// #endif
            //<docsnippet_startup>
            var host = new HostBuilder()
                //<docsnippet_configure_app>
                .ConfigureAppConfiguration(c =>
                {
                    c.AddCommandLine(args);
                })
                //</docsnippet_configure_app>
                //<docsnippet_middleware>
                .ConfigureFunctionsWorker((c, b) =>
                {
                    b.UseFunctionExecutionMiddleware();
                })
                //</docsnippet_middleware>
                //<docsnippet_dependency_injection>
                .ConfigureServices(s =>
                {
                    s.AddSingleton<IHttpResponderService, DefaultHttpResponderService>();
                })
                //</docsnippet_dependency_injection>
                .Build();
            //</docsnippet_startup>

            //<docsnippet_host_run>
            await host.RunAsync();
            //</docsnippet_host_run>
        }
    }
}
