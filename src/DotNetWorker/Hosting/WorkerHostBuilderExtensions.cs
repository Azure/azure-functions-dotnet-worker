// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Provides extension methods to work with a <see cref="IHostBuilder"/>.
    /// </summary>
    public static class WorkerHostBuilderExtensions
    {
        /// <summary>
        /// Configures the Azure Functions Worker.
        /// </summary>
        /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder ConfigureFunctionsWorker(this IHostBuilder builder)
        {
            return builder.ConfigureFunctionsWorker(o => { });
        }

        /// <summary>
        /// Configures the Azure Functions Worker, with a delegate to configure a provided <see cref="IFunctionsWorkerApplicationBuilder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="configure">A delegate that will be invoked to configure the provided <see cref="IFunctionsWorkerApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder ConfigureFunctionsWorker(this IHostBuilder builder, Action<IFunctionsWorkerApplicationBuilder> configure)
        {
            return builder.ConfigureFunctionsWorker(configure);
        }

        /// <summary>
        /// Configures the Azure Functions Worker, with a delegate to configure a provided <see cref="IFunctionsWorkerApplicationBuilder"/>
        /// and a delegate to configure the <see cref="WorkerOptions"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="configure">A delegate that will be invoked to configure the provided <see cref="IFunctionsWorkerApplicationBuilder"/>.</param>
        /// <param name="configureOptions">A delegate that will be invoked to configure the provided <see cref="WorkerOptions"/>.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder ConfigureFunctionsWorker(this IHostBuilder builder, Action<IFunctionsWorkerApplicationBuilder> configure, Action<WorkerOptions> configureOptions)
        {
            return builder.ConfigureFunctionsWorker((context, b) => configure(b), configureOptions);
        }

        /// <summary>
        /// Configures the Azure Functions Worker, with a delegate to configure a provided <see cref="HostBuilderContext"/> and an <see cref="IFunctionsWorkerApplicationBuilder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="configure">A delegate that will be invoked to configure the provided <see cref="HostBuilderContext"/> and an <see cref="IFunctionsWorkerApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder ConfigureFunctionsWorker(this IHostBuilder builder, Action<HostBuilderContext, IFunctionsWorkerApplicationBuilder> configure)
        {
            return builder.ConfigureFunctionsWorker(configure, o => { });
        }

        /// <summary>
        /// Configures the Azure Functions Worker, with a delegate to configure a provided <see cref="HostBuilderContext"/> and an <see cref="IFunctionsWorkerApplicationBuilder"/>,
        /// and a delegate to configure the <see cref="WorkerOptions"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="configure">A delegate that will be invoked to configure the provided <see cref="HostBuilderContext"/> and an <see cref="IFunctionsWorkerApplicationBuilder"/>.</param>
        /// <param name="configureOptions">A delegate that will be invoked to configure the provided <see cref="WorkerOptions"/>.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder ConfigureFunctionsWorker(this IHostBuilder builder, Action<HostBuilderContext, IFunctionsWorkerApplicationBuilder> configure, Action<WorkerOptions> configureOptions)
        {
            builder.ConfigureServices((context, services) =>
            {
                IFunctionsWorkerApplicationBuilder appBuilder = services.AddFunctionsWorker(configureOptions);

                configure(context, appBuilder);
            });

            return builder;
        }
    }
}
