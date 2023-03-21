// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Linq;
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
            System.Diagnostics.Debugger.Launch();
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
                            // This property configuration serves solely for demonstration purposes, and it is not mandatory to replicate the same property and its associated value in your own application.
                            options.ApplicationVersion = "v1";
                        })
                        .AddApplicationInsightsLogger();
                })
                //</docsnippet_configure_defaults>
                //<docsnippet_dependency_injection>
                .ConfigureServices(s =>
                {
                    s.AddSingleton<IHttpResponderService, DefaultHttpResponderService>();
                    s.Configure<LoggerFilterOptions>(options =>
                    {
                        // The Application Insights SDK adds a default logging filter that instructs ILogger to capture only Warning and more severe logs. Application Insights requires an explicit override.
                        // Log levels can also be configured using appsettings.json. For more information, see https://learn.microsoft.com/en-us/azure/azure-monitor/app/worker-service#ilogger-logs
                        LoggerFilterRule toRemove = options.Rules.FirstOrDefault(rule => rule.ProviderName
                            == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

                        if (toRemove is not null)
                        {
                            options.Rules.Remove(toRemove);
                        }
                    });
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
