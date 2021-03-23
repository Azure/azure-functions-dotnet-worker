// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Provides extension methods to work with a <see cref="IHostBuilder"/>.
    /// </summary>
    public static class WorkerHostBuilderExtensions
    {
        /// <summary>
        /// Configures the default set of Functions Worker services to the provided <see cref="IHostBuilder"/>.
        /// The following defaults are configured:
        /// <list type="bullet">
        ///     <item><description>A default set of converters.</description></item>
        ///     <item><description>Configures the default <see cref="JsonSerializerOptions"/> to ignore casing on property names.</description></item>
        ///     <item><description>Integration with Azure Functions logging.</description></item>
        ///     <item><description>Adds environment variables as a configuration source.</description></item>
        ///     <item><description>Adds command line arguments as a configuration source.</description></item>
        ///     <item><description>Output binding middleware and features.</description></item>
        ///     <item><description>Function execution middleware.</description></item>
        ///     <item><description>Default gRPC support.</description></item>
        /// </list>
        /// </summary>
        /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder ConfigureFunctionsWorkerDefaults(this IHostBuilder builder)
        {
            return builder.ConfigureFunctionsWorkerDefaults(o => { });
        }

        /// <summary>
        /// Configures the default set of Functions Worker services to the provided <see cref="IHostBuilder"/>,
        /// with a delegate to configure a provided <see cref="IFunctionsWorkerApplicationBuilder"/>.
        /// The following defaults are configured:
        /// <list type="bullet">
        ///     <item><description>A default set of converters.</description></item>
        ///     <item><description>Configures the default <see cref="JsonSerializerOptions"/> to ignore casing on property names.</description></item>
        ///     <item><description>Integration with Azure Functions logging.</description></item>
        ///     <item><description>Adds environment variables as a configuration source.</description></item>
        ///     <item><description>Adds command line arguments as a configuration source.</description></item>
        ///     <item><description>Output binding middleware and features.</description></item>
        ///     <item><description>Function execution middleware.</description></item>
        ///     <item><description>Default gRPC support.</description></item>
        /// </list>
        /// </summary>
        /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="configure">A delegate that will be invoked to configure the provided <see cref="IFunctionsWorkerApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder ConfigureFunctionsWorkerDefaults(this IHostBuilder builder, Action<IFunctionsWorkerApplicationBuilder> configure)
        {
            return builder.ConfigureFunctionsWorkerDefaults(configure, o => { });
        }

        /// <summary>
        /// Configures the default set of Functions Worker services to the provided <see cref="IHostBuilder"/>,
        /// with a delegate to configure a provided <see cref="IFunctionsWorkerApplicationBuilder"/> and a delegate to configure the <see cref="WorkerOptions"/>.
        /// The following defaults are configured:
        /// <list type="bullet">
        ///     <item><description>A default set of converters.</description></item>
        ///     <item><description>Configures the default <see cref="JsonSerializerOptions"/> to ignore casing on property names.</description></item>
        ///     <item><description>Integration with Azure Functions logging.</description></item>
        ///     <item><description>Adds environment variables as a configuration source.</description></item>
        ///     <item><description>Adds command line arguments as a configuration source.</description></item>
        ///     <item><description>Output binding middleware and features.</description></item>
        ///     <item><description>Function execution middleware.</description></item>
        ///     <item><description>Default gRPC support.</description></item>
        /// </list>
        /// </summary>
        /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="configure">A delegate that will be invoked to configure the provided <see cref="IFunctionsWorkerApplicationBuilder"/>.</param>
        /// <param name="configureOptions">A delegate that will be invoked to configure the provided <see cref="WorkerOptions"/>.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder ConfigureFunctionsWorkerDefaults(this IHostBuilder builder, Action<IFunctionsWorkerApplicationBuilder> configure, Action<WorkerOptions> configureOptions)
        {
            return builder.ConfigureFunctionsWorkerDefaults((context, b) => configure(b), configureOptions);
        }

        /// <summary>
        /// Configures the default set of Functions Worker services to the provided <see cref="IHostBuilder"/>,
        /// with a delegate to configure a provided <see cref="HostBuilderContext"/> and an <see cref="IFunctionsWorkerApplicationBuilder"/>.
        /// <list type="bullet">
        ///     <item><description>A default set of converters.</description></item>
        ///     <item><description>Configures the default <see cref="JsonSerializerOptions"/> to ignore casing on property names.</description></item>
        ///     <item><description>Integration with Azure Functions logging.</description></item>
        ///     <item><description>Adds environment variables as a configuration source.</description></item>
        ///     <item><description>Adds command line arguments as a configuration source.</description></item>
        ///     <item><description>Output binding middleware and features.</description></item>
        ///     <item><description>Function execution middleware.</description></item>
        ///     <item><description>Default gRPC support.</description></item>
        /// </list>
        /// </summary>
        /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="configure">A delegate that will be invoked to configure the provided <see cref="HostBuilderContext"/> and an <see cref="IFunctionsWorkerApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder ConfigureFunctionsWorkerDefaults(this IHostBuilder builder, Action<HostBuilderContext, IFunctionsWorkerApplicationBuilder> configure)
        {
            return builder.ConfigureFunctionsWorkerDefaults(configure, o => { });
        }

        /// <summary>
        /// Configures the default set of Functions Worker services to the provided <see cref="IHostBuilder"/>,
        /// with a delegate to configure a provided <see cref="HostBuilderContext"/> and an <see cref="IFunctionsWorkerApplicationBuilder"/>,
        /// and a delegate to configure the <see cref="WorkerOptions"/>.
        /// <list type="bullet">
        ///     <item><description>A default set of converters.</description></item>
        ///     <item><description>Configures the default <see cref="JsonSerializerOptions"/> to ignore casing on property names.</description></item>
        ///     <item><description>Integration with Azure Functions logging.</description></item>
        ///     <item><description>Adds environment variables as a configuration source.</description></item>
        ///     <item><description>Adds command line arguments as a configuration source.</description></item>
        ///     <item><description>Output binding middleware and features.</description></item>
        ///     <item><description>Function execution middleware.</description></item>
        ///     <item><description>Default gRPC support.</description></item>
        /// </list>
        /// </summary>
        /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="configure">A delegate that will be invoked to configure the provided <see cref="HostBuilderContext"/> and an <see cref="IFunctionsWorkerApplicationBuilder"/>.</param>
        /// <param name="configureOptions">A delegate that will be invoked to configure the provided <see cref="WorkerOptions"/>.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder ConfigureFunctionsWorkerDefaults(this IHostBuilder builder, Action<HostBuilderContext, IFunctionsWorkerApplicationBuilder> configure, Action<WorkerOptions> configureOptions)
        {
            builder
                .ConfigureAppConfiguration(configBuilder =>
                {
                    var cmdLine = Environment.GetCommandLineArgs();
                    RegisterCommandLine(configBuilder, cmdLine);

                    configBuilder.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    IFunctionsWorkerApplicationBuilder appBuilder = services.AddFunctionsWorkerDefaults(configureOptions);

                    // Call the provided configuration prior to adding default middleware
                    configure(context, appBuilder);

                    // Add default middleware
                    appBuilder.UseDefaultWorkerMiddleware();
                });

            return builder;
        }

        internal static void RegisterCommandLine(IConfigurationBuilder builder, string[] cmdLine)
        {
            if (cmdLine.Length > 0 &&
                !cmdLine[0].StartsWith("--"))
            {
                cmdLine[0] = $"\"{cmdLine[0]}\"";
            }

            builder.AddCommandLine(cmdLine);
        }
    }
}
