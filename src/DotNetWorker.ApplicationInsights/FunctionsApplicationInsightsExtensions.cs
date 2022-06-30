using System;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Azure.Functions.Worker.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace Microsoft.Azure.Functions.Worker
{
    public static class FunctionsApplicationInsightsExtensions
    {
        /// <summary>
        /// Adds Application Insights support by internally calling <see cref="ApplicationInsightsExtensions.AddApplicationInsightsTelemetryWorkerService(IServiceCollection)"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IFunctionsWorkerApplicationBuilder"/></param>
        /// <param name="configureOptions">ction to configure ApplicationInsights services.</param>
        /// <returns>The <see cref="IFunctionsWorkerApplicationBuilder"/></returns>
        public static IFunctionsWorkerApplicationBuilder AddApplicationInsights(this IFunctionsWorkerApplicationBuilder builder, Action<ApplicationInsightsServiceOptions>? configureOptions = null)
        {
            builder.Services.AddCommonServices();

            builder.Services.AddApplicationInsightsTelemetryWorkerService(options =>
            {
                configureOptions?.Invoke(options);
            });

            return builder;
        }

        /// <summary>
        /// Adds the <see cref="ApplicationInsightsLoggerProvider"/> and disables the Functions host passthrough logger.
        /// </summary>
        /// <param name="builder">The <see cref="IFunctionsWorkerApplicationBuilder"/></param>
        /// <param name="configureOptions">Action to configure ApplicationInsights logger.</param>
        /// <returns>The <see cref="IFunctionsWorkerApplicationBuilder"/></returns>
        public static IFunctionsWorkerApplicationBuilder AddApplicationInsightsLogger(this IFunctionsWorkerApplicationBuilder builder, Action<ApplicationInsightsLoggerOptions>? configureOptions = null)
        {
            builder.Services.AddCommonServices();

            // Use the App Insights Logger directly
            builder.Services.AddOptions<WorkerOptions>().Configure(options => options.DisableHostLogger = true);
            builder.Services.AddLogging(logging =>
            {
                logging.AddApplicationInsights(options =>
                {
                    options.IncludeScopes = false;
                    configureOptions?.Invoke(options);
                });
            });

            return builder;
        }

        private static IServiceCollection AddCommonServices(this IServiceCollection services)
        {
            services.TryAddEnumerable(new ServiceDescriptor(typeof(ITelemetryInitializer), typeof(FunctionsTelemetryInitializer), ServiceLifetime.Singleton));
            services.TryAddEnumerable(new ServiceDescriptor(typeof(ITelemetryModule), typeof(FunctionsTelemetryModule), ServiceLifetime.Singleton));

            return services;
        }
    }
}
