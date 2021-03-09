// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Azure Functions extensions for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the core set of services for the Azure Functions worker.
        /// This call also adds the default set of binding converters and gRPC support.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">The action used to configure <see cref="WorkerOptions"/>.</param>
        /// <returns>The same <see cref="IFunctionsWorkerApplicationBuilder"/> for chaining.</returns>
        public static IFunctionsWorkerApplicationBuilder AddFunctionsWorkerDefaults(this IServiceCollection services, Action<WorkerOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Converters
            services.RegisterDefaultConverters();

            // Core services registration
            var builder = services.AddFunctionsWorkerCore(configure);

            // gRPC support
            services.AddGrpc();

            return builder;
        }
    }
}
