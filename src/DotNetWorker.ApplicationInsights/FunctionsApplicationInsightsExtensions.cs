// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITelemetryInitializer, FunctionsTelemetryInitializer>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITelemetryModule, FunctionsTelemetryModule>());

            // Lets the host know that the worker is sending logs to App Insights. The host will now ignore these.
            services.Configure<WorkerOptions>(workerOptions => workerOptions.Capabilities["WorkerApplicationInsightsLoggingEnabled"] = bool.TrueString);
            return services;
        }
    }
}
