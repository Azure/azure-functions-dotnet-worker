// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.Azure.Functions.Worker.ApplicationInsights;
using Microsoft.Azure.Functions.Worker.ApplicationInsights.Initializers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker
{
    public static class FunctionsApplicationInsightsExtensions
    {
        public static IServiceCollection ConfigureFunctionsApplicationInsights(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            services.AddSingleton<IConfigureOptions<FunctionsApplicationInsightsOptions>, FunctionsApplicationInsightsOptionsInitializer>();

            services.AddSingleton<IConfigureOptions<AppServiceOptions>, AppServiceOptionsInitializer>();
            services.AddSingleton<AppServiceEnvironmentVariableMonitor>();
            services.AddSingleton<IOptionsChangeTokenSource<AppServiceOptions>>(p => p.GetRequiredService<AppServiceEnvironmentVariableMonitor>());
            services.AddSingleton<IHostedService>(p => p.GetRequiredService<AppServiceEnvironmentVariableMonitor>());

            services.AddSingleton<FunctionsRoleInstanceProvider>();

            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITelemetryInitializer, FunctionsTelemetryInitializer>());
            services.AddSingleton<ITelemetryInitializer>(provider =>
            {
                // To match parity with the Host, we need to update the QuickPulseTelemetryModule.ServerId. We don't want to reference the
                // top-level WorkerService or AspNetCore packages, so we cannot use ConfigureTelemetryModules().
                // 
                // Nesting this setup inside this ITelemetryInitializer factory as it guarantees it will be run before
                // any ITelemetryModules are initialized.
                var modules = provider.GetServices<ITelemetryModule>();
                var quickPulseModule = modules.OfType<QuickPulseTelemetryModule>().SingleOrDefault();
                if (quickPulseModule is not null)
                {
                    var roleInstanceProvider = provider.GetRequiredService<FunctionsRoleInstanceProvider>();
                    quickPulseModule.ServerId = roleInstanceProvider.GetRoleInstanceName();
                }

                return ActivatorUtilities.CreateInstance<FunctionsRoleEnvironmentTelemetryInitializer>(provider);
            });
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITelemetryModule, FunctionsTelemetryModule>());
            
            services.AddHostedService<ApplicationInsightsValidationService>();

            // Lets the host know that the worker is sending logs to App Insights. The host will now ignore these.
            services.Configure<WorkerOptions>(workerOptions => workerOptions.Capabilities["WorkerApplicationInsightsLoggingEnabled"] = bool.TrueString);
            return services;
        }

        /// <summary>
        /// This services is for a singular purpose: trigger validation of <see cref="FunctionsApplicationInsightsOptions" /> on startup.
        /// </summary>
        private class ApplicationInsightsValidationService : IHostedService
        {
            public ApplicationInsightsValidationService(IOptions<FunctionsApplicationInsightsOptions> options, IServiceProvider sp)
            {
                if (options.Value == null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                if (sp.GetService<TelemetryClient>() == null)
                {
                    throw new OptionsValidationException(nameof(TelemetryClient),
                        typeof(FunctionsApplicationInsightsOptions),
                        new[] { "Application Insights SDK has not been added. Please add and configure the Application Insights SDK. See https://learn.microsoft.com/en-us/azure/azure-monitor/app/worker-service for more information." });
                }
            }

            public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }    
}
