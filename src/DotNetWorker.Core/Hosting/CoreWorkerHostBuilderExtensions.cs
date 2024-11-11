﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
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

        /// <summary>
        /// Invokes auto-generated configuration methods for a given <see cref="IHostBuilder"/>.
        /// This method searches for classes that implement the <see cref="IAutoConfigureStartup"/> interface,
        /// excluding interfaces and abstract classes. For each identified class, it locates the
        /// <c>Configure</c> method with the signature <c>void Configure(IHostBuilder hostBuilder)</c>,
        /// and executes the method using an instance of the class.
        /// </summary>
        /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <returns>The same <see cref="IHostBuilder"/> after the auto-generated configuration methods are invoked.</returns>
        public static IHostBuilder InvokeAutoGeneratedConfigureMethods(this IHostBuilder builder)
        {
            Assembly? entry = Assembly.GetEntryAssembly();
            if (entry is null)
            {
                return builder; // This may be null in tests.
            }

            var autoConfigureStartupTypes = entry
                .GetTypes()
                .Where(t => typeof(IAutoConfigureStartup).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in autoConfigureStartupTypes)
            {
                var instance = (IAutoConfigureStartup)Activator.CreateInstance(type)!;
                instance.Configure(builder);
            }

            return builder;
        }
    }
}
