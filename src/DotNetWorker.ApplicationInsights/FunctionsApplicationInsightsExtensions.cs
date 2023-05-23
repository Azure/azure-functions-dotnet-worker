// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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
        /// <param name="configureOptions">Action to configure ApplicationInsights services.</param>
        /// <returns>The <see cref="IFunctionsWorkerApplicationBuilder"/></returns>
        public static IFunctionsWorkerApplicationBuilder AddApplicationInsights(this IFunctionsWorkerApplicationBuilder builder, Action<ApplicationInsightsServiceOptions>? configureOptions = null)
        {
            builder.AddCommonServices();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITelemetryModule, FunctionsTelemetryModule>());

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
            builder.AddCommonServices();

            // Lets the host know that the worker is sending logs to App Insights. The host will now ignore these.
            builder.Services.Configure<WorkerOptions>(workerOptions => workerOptions.Capabilities["WorkerApplicationInsightsLoggingEnabled"] = bool.TrueString);

            builder.Services.AddLogging(logging =>
            {
                logging.AddApplicationInsights(options =>
                {
                    configureOptions?.Invoke(options);
                });
            });

            return builder;
        }

        private static IFunctionsWorkerApplicationBuilder AddCommonServices(this IFunctionsWorkerApplicationBuilder builder)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITelemetryInitializer, FunctionsTelemetryInitializer>());

            return builder;
        }
    }
}
