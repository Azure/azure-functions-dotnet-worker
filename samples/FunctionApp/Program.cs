// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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

                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(s =>
                {
                    //s.AddApplicationInsightsTelemetryWorkerService();
                    //s.ConfigureFunctionsApplicationInsights();

                    
                    
                    s.AddSingleton<IHttpResponderService, DefaultHttpResponderService>();
                    //s.AddOpenTelemetry().UseAzureMonitor();
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

                    //s.ConfigureFunctionsOpenTelemetry();
                    //s.AddOpenTelemetry()
                    //.ConfigureFunctionsOpenTelemetry()
                    //.ConfigureResource(configureResource)
                    //.WithMetrics(builder =>
                    //{
                    //    builder.AddMeter("Azure.Functions");
                    //    //builder.AddConsoleExporter();
                    //    builder.AddAzureMonitorMetricExporter(o =>
                    //    {
                    //        o.ConnectionString = "<>";
                    //    });
                    //    builder.AddOtlpExporter();
                    //})
                    //.WithTracing(builder =>
                    //{
                    //    //builder.AddAspNetCoreInstrumentation();
                    //    builder.AddHttpClientInstrumentation();
                    //    builder.AddSource("Hola");
                    //    builder.AddConsoleExporter();
                    //    //builder.AddGrpcClientInstrumentation();

                    //    //builder.AddSource("Durable");
                    //    //builder.ConfigureResource(x => x.AddDetector(new FunctionsResourceDetector()));
                    //    builder.AddAzureMonitorTraceExporter();
                    //    builder.AddOtlpExporter();
                    //    //builder.AddZipkinExporter(o => o.HttpClientFactory = () =>
                    //    //{
                    //    //    HttpClient client = new HttpClient();
                    //    //    client.DefaultRequestHeaders.Add("X-MyCustomHeader", "value");
                    //    //    return client;
                    //    //});
                    //});
                    s.AddOpenTelemetry()
                    .ConfigureFunctions()
                    .WithTracing(builder =>
                    {
                        builder.AddAspNetCoreInstrumentation();
                        builder.AddHttpClientInstrumentation();
                        builder.AddAzureMonitorTraceExporter();
                        builder.AddOtlpExporter();
                    });

                })
                .ConfigureLogging((context, loggingBuilder) =>
                {
                    loggingBuilder.AddOpenTelemetry(options =>
                    {
                        // Configure OpenTelemetry logging
                        options.IncludeScopes = true;

                        // Assuming OTLP as the protocol for logs as well
                        options.AddOtlpExporter();
                        options.AddAzureMonitorLogExporter();
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
