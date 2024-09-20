// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Reflection;
using Microsoft.Extensions.Configuration;
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
            return builder.ConfigureFunctionsWorkerDefaults(configure: o => { });
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
        /// with a delegate to configure a provided <see cref="WorkerOptions"/>.
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
        /// <param name="configureOptions">A delegate that will be invoked to configure the provided <see cref="WorkerOptions"/>.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder ConfigureFunctionsWorkerDefaults(this IHostBuilder builder, Action<WorkerOptions> configureOptions)
        {
            return builder.ConfigureFunctionsWorkerDefaults(o => { }, configureOptions);
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
                .ConfigureHostConfiguration(config =>
                {
                    // Add AZURE_FUNCTIONS_ prefixed environment variables
                    config.AddEnvironmentVariables("AZURE_FUNCTIONS_");
                })
                .ConfigureAppConfiguration((context, configBuilder) =>
                {
                    if (context.HostingEnvironment.IsDevelopment() && Assembly.GetEntryAssembly() is Assembly assembly)
                    {
                        try
                        {
                            configBuilder.AddUserSecrets(assembly, optional: true);
                        }
                        catch (FileNotFoundException)
                        {
                            // The assembly cannot be found, so just skip it.
                        }
                    }

                    configBuilder.AddEnvironmentVariables();

                    var cmdLine = Environment.GetCommandLineArgs();
                    RegisterCommandLine(configBuilder, cmdLine);
                })
                .ConfigureServices((context, services) =>
                {
                    IFunctionsWorkerApplicationBuilder appBuilder = services.AddFunctionsWorkerDefaults(configureOptions);

                    // Call the provided configuration prior to adding default middleware
                    configure(context, appBuilder);

                    // Add default middleware
                    appBuilder.UseDefaultWorkerMiddleware();
                })
               .UseDefaultServiceProvider((context, options) =>
                {
                    options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
                });

            // Invoke any extension methods auto generated by functions worker sdk.
            builder.InvokeAutoGeneratedConfigureMethods();

            return builder;
        }

        internal static void RegisterCommandLine(IConfigurationBuilder builder, string[] cmdLine)
        {
            // If either of the first two arguments do not begin with '--', wrap them in
            // quotes. On Linux, either of these first two arguments can be the path to the
            // assembly, which begins with a '/' and is interpreted as a switch.
            for (int i = 0; i <= 1; i++)
            {
                if (cmdLine.Length <= i)
                {
                    break;
                }

                string arg = cmdLine[i];

                if (arg.StartsWith("--"))
                {
                    break;
                }

                cmdLine[i] = $"\"{arg}\"";
            }

            var switchMappings = new Dictionary<string, string>
            {
                { "--functions-uri", "Functions:Worker:HostEndpoint" },
                { "--functions-request-id", "Functions:Worker:RequestId" },
                { "--functions-worker-id", "Functions:Worker:WorkerId" },
                { "--functions-grpc-max-message-length", "Functions:Worker:GrpcMaxMessageLength" },
            };
            builder.AddCommandLine(cmdLine, switchMappings);
        }
    }
}
