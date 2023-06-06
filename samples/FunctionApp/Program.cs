// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace FunctionApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // #if DEBUG
            //     Debugger.Launch();
            // #endif
            //<docsnippet_startup>
            var host = new HostBuilder()
                //<docsnippet_configure_defaults>
                .ConfigureFunctionsWorkerDefaults(builder =>
                {
                    builder
                        .AddApplicationInsights(options =>
                        {
                            // Configure the underlying ApplicationInsightsServiceOptions
                            options.EnableAdaptiveSampling = false;
                        })
                        .AddApplicationInsightsLogger();
                })
                // Application Insights collects these ILogger logs, with a severity of Warning or above by default, and dependencies.
                .ConfigureLogging(logging => logging
                    .AddFilter<ApplicationInsightsLoggerProvider>(null, LogLevel.Information))
                //</docsnippet_configure_defaults>
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
