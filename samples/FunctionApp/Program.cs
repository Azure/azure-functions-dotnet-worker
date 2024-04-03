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
            var host = new HostBuilder()
                .ConfigureFunctionsWebApplication()
                .ConfigureServices(s =>
                {

                    // Classic SDK
                    //s.AddApplicationInsightsTelemetryWorkerService();
                    //s.ConfigureFunctionsApplicationInsights();

                    // OTEL
                    s.AddOpenTelemetry()
                    .UseFunctionsWorkerDefaults()
                    .WithTracing(builder =>
                    {
                        builder.AddHttpClientInstrumentation();
                        builder.AddOtlpExporter();
                    })
                    .UseAzureMonitor();

                    s.AddSingleton<IHttpResponderService, DefaultHttpResponderService>();                    

                    s.AddHttpClient();
                })

                .ConfigureLogging((context, loggingBuilder) =>
                {
                    loggingBuilder.AddOpenTelemetry(options =>
                    {
                        options.IncludeScopes = true;
                        options.AddOtlpExporter();
                    });
                })

                .Build();
            await host.RunAsync();
        }
    }
}
