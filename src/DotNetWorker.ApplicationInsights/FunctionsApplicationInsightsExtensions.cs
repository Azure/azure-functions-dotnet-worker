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

                // removing the logging filter auto-added by App Insights
                // https://github.com/microsoft/ApplicationInsights-dotnet/blob/f4389840e435290fadf7fb7555661e8759070682/NETCORE/src/Shared/Extensions/ApplicationInsightsExtensions.cs#L421-L428
                logging.Services.Configure<LoggerFilterOptions>(options =>
                    {
                        var rule = options.Rules.FirstOrDefault(r => r.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
                        if (rule is not null)
                        {
                            options.Rules.Remove(rule);
                        }
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

            return builder;
        }
    }
}
