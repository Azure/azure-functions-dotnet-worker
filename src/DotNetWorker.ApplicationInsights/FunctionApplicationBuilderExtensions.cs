using System;
using DotNetWorker.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Azure.Functions.Worker.ApplicationInsights;
using Microsoft.Azure.WebJobs.Logging.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker
{
    public static class FunctionsWorkerApplicationBuilderExtensions
    {
        public static IFunctionsWorkerApplicationBuilder AddApplicationInsights(this IFunctionsWorkerApplicationBuilder builder, Action<ApplicationInsightsServiceOptions>? configureOptions = null)
        {
            ApplicationInsightsServiceOptions options = new ApplicationInsightsServiceOptions
            {
                ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")
            };

            configureOptions?.Invoke(options);

            builder.Services.AddApplicationInsightsTelemetryWorkerService(options);
            builder.Services.AddSingleton<ISdkVersionProvider, FunctionsWorkerVersionProvider>();
            builder.Services.AddSingleton<IRoleInstanceProvider, FunctionsWorkerRoleInstanceProvider>();
            builder.Services.AddSingleton<ITelemetryInitializer, FunctionsTelemetryInitializer>();
            builder.Services.AddSingleton<ITelemetryModule, FunctionsWorkerTelemetryModule>();

            return builder;
        }
    }
}
