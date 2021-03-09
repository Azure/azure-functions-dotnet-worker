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
    public static class CoreWorkerHostBuilderExtensions
    {
        /// <summary>
        /// Configures the core set of Functions Worker services to the provided <see cref="IHostBuilder"/>,
        /// with a delegate to configure a provided <see cref="HostBuilderContext"/> and an <see cref="IFunctionsWorkerApplicationBuilder"/>,
        /// and a delegate to configure the <see cref="WorkerOptions"/>.
        /// NOTE: You must configure required services for an operational worker when using this method.
        /// </summary>
        /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="configure">A delegate that will be invoked to configure the provided <see cref="HostBuilderContext"/> and an <see cref="IFunctionsWorkerApplicationBuilder"/>.</param>
        /// <param name="configureOptions">A delegate that will be invoked to configure the provided <see cref="WorkerOptions"/>.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder ConfigureFunctionsWorker(this IHostBuilder builder, Action<HostBuilderContext, IFunctionsWorkerApplicationBuilder> configure, Action<WorkerOptions> configureOptions)
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.ConfigureServices((context, services) =>
            {
                IFunctionsWorkerApplicationBuilder appBuilder = services.AddFunctionsWorkerCore(configureOptions);

                configure(context, appBuilder);
            });

            return builder;
        }
    }
}
