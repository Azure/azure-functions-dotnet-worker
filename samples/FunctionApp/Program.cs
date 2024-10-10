// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Linq;
using FunctionApp;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

//<docsnippet_startup>
//<docsnippet_configure_defaults>
FunctionsApplicationBuilder funcBuilder = FunctionsApplication.CreateBuilder(args);
//</docsnippet_configure_defaults>

//<docsnippet_dependency_injection>
funcBuilder.Services.AddApplicationInsightsTelemetryWorkerService();
funcBuilder.Services.ConfigureFunctionsApplicationInsights();
funcBuilder.Services.AddSingleton<IHttpResponderService, DefaultHttpResponderService>();
funcBuilder.Services.Configure<LoggerFilterOptions>(options =>
{
    // The Application Insights SDK adds a default logging filter that instructs ILogger to capture only Warning and more severe logs. Application Insights requires an explicit override.
    // Log levels can also be configured using appsettings.json. For more information, see https://learn.microsoft.com/en-us/azure/azure-monitor/app/worker-service#ilogger-logs
    LoggerFilterRule toRemove = options.Rules.FirstOrDefault(rule => rule.ProviderName
        == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
    FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);

    if (toRemove is not null)
    {
        options.Rules.Remove(toRemove);
    }
});
//</docsnippet_dependency_injection

IHost app = funcBuilder.Build();
//</docsnippet_startup>

//<docsnippet_host_run>
await app.RunAsync();
//</docsnippet_host_run>
