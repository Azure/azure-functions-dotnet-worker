// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Azure.Functions.Worker.ApplicationInsights;
using Microsoft.Azure.Functions.Worker.Logging;
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

        private static IFunctionsWorkerApplicationBuilder AddCommonServices(this IFunctionsWorkerApplicationBuilder builder)
        {
            builder.Services.TryAddEnumerable(new ServiceDescriptor(typeof(ITelemetryInitializer), typeof(FunctionsTelemetryInitializer), ServiceLifetime.Singleton));
            builder.Services.TryAddEnumerable(new ServiceDescriptor(typeof(ITelemetryModule), typeof(FunctionsTelemetryModule), ServiceLifetime.Singleton));

            // User logs will be written directly to Application Insights; this prevents duplicate logging.
            builder.Services.AddSingleton<IUserLogWriter>(_ => NullUserLogWriter.Instance);

            // This middleware is temporary for the preview. Eventually this behavior will move into the
            // core worker assembly.
            if (!builder.Services.Any(p => p.ImplementationType == typeof(FunctionActivitySourceMiddleware)))
            {
                builder.Services.AddSingleton<FunctionActivitySourceMiddleware>();
                builder.Use(next =>
                {
                    return async context =>
                    {
                        var middleware = context.InstanceServices.GetRequiredService<FunctionActivitySourceMiddleware>();
                        await middleware.Invoke(context, next);
                    };
                });
            }

            return builder;
        }
    }
}
