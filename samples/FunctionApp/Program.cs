// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace FunctionApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                //.ConfigureFunctionsWorkerDefaults()
                .ConfigureFunctionsWebApplication()
                .ConfigureServices(s =>
                {
                    //s.AddApplicationInsightsTelemetryWorkerService();
                    //s.ConfigureFunctionsApplicationInsights();
                    //s.AddSingleton<IHttpResponderService, DefaultHttpResponderService>();
                    //s.Configure<LoggerFilterOptions>(options =>
                    //{
                    //    // The Application Insights SDK adds a default logging filter that instructs ILogger to capture only Warning and more severe logs. Application Insights requires an explicit override.
                    //    // Log levels can also be configured using appsettings.json. For more information, see https://learn.microsoft.com/en-us/azure/azure-monitor/app/worker-service#ilogger-logs
                    //    LoggerFilterRule toRemove = options.Rules.FirstOrDefault(rule => rule.ProviderName
                    //        == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

                    //    if (toRemove is not null)
                    //    {
                    //        options.Rules.Remove(toRemove);
                    //    }
                    //});

                    // OTEL
                    s.AddOpenTelemetry()
                    .UseFunctionsWorkerDefaults()                    
                    .UseAzureMonitor();

                    s.AddOpenTelemetry().UseOtlpExporter();
                  
                    s.AddSingleton<IHttpResponderService, DefaultHttpResponderService>();
                    s.AddHttpClient();
                })
                .ConfigureLogging(b => b.
                AddOpenTelemetry()
                
                )
                .Build();
            await host.RunAsync();
        }
    }
}
