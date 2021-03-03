// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Channels;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.FunctionRpc;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Azure Functions extensions for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services for the Azure Functions worker.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">The action used to configure <see cref="WorkerOptions"/>.</param>
        /// <returns></returns>
        public static IFunctionsWorkerApplicationBuilder AddFunctionsWorker(this IServiceCollection services, Action<WorkerOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Converters
            services.RegisterDefaultConverters();

            // Channels
            services.RegisterOutputChannel();

            // Internal logging            
            services.AddLogging(logging =>
            {
                logging.Services.AddSingleton<ILoggerProvider, GrpcFunctionsHostLoggerProvider>();
            });

            // Request handling
            services.AddSingleton<IFunctionsHostClient, DefaultFunctionsHostClient>();
            services.AddSingleton<IHostRequestHandler, DefaultHostRequestHandler>();
            services.AddSingleton<IFunctionBroker, FunctionBroker>();

            // Execution
            services.AddSingleton<IMethodInfoLocator, DefaultMethodInfoLocator>();
            services.AddSingleton<IFunctionInvokerFactory, DefaultFunctionInvokerFactory>();
            services.AddSingleton<IMethodInvokerFactory, DefaultMethodInvokerFactory>();
            services.AddSingleton<IFunctionActivator, DefaultFunctionActivator>();
            services.AddSingleton<IFunctionExecutor, DefaultFunctionExecutor>();

            // Function Execution Contexts
            services.AddSingleton<IFunctionContextFactory, DefaultFunctionContextFactory>();

            // Invocation Features
            services.TryAddSingleton<IInvocationFeaturesFactory, DefaultInvocationFeaturesFactory>();
            services.AddSingleton<IInvocationFeatureProvider, DefaultBindingFeatureProvider>();

            // Function Definition
            services.AddSingleton<IFunctionDefinitionFactory, DefaultFunctionDefinitionFactory>();

            // Output Bindings
            services.AddSingleton<IOutputBindingsInfoProvider, DefaultOutputBindingsInfoProvider>();

            // gRpc
            services.AddSingleton<FunctionRpcClient>(p =>
            {
                IOptions<WorkerStartupOptions> argumentsOptions = p.GetService<IOptions<WorkerStartupOptions>>();
                WorkerStartupOptions arguments = argumentsOptions.Value;

                GrpcChannel grpcChannel = GrpcChannel.ForAddress($"http://{arguments.Host}:{arguments.Port}", new GrpcChannelOptions()
                {
                    Credentials = ChannelCredentials.Insecure
                });

                return new FunctionRpcClient(grpcChannel);
            });
            services.AddSingleton<IHostedService, WorkerHostedService>();

            // Options
            services.AddOptions<WorkerStartupOptions>()
                .Configure<IConfiguration>((arguments, config) =>
                {
                    config.Bind(arguments);
                });

            if (configure != null)
            {
                services.Configure(configure);
            }

            return new FunctionsWorkerApplicationBuilder(services);
        }

        internal static IServiceCollection RegisterDefaultConverters(this IServiceCollection services)
        {
            return services.AddSingleton<IConverter, FunctionContextConverter>()
                           .AddSingleton<IConverter, TypeConverter>()
                           .AddSingleton<IConverter, MemoryConverter>()
                           .AddSingleton<IConverter, StringToByteConverter>()
                           .AddSingleton<IConverter, JsonPocoConverter>()
                           .AddSingleton<IConverter, EnumerableConverter>();
        }

        internal static IServiceCollection RegisterOutputChannel(this IServiceCollection services)
        {
            return services.AddSingleton<FunctionsHostOutputChannel>(s =>
            {
                UnboundedChannelOptions outputOptions = new UnboundedChannelOptions
                {
                    SingleWriter = false,
                    SingleReader = true,
                    AllowSynchronousContinuations = true
                };

                return new FunctionsHostOutputChannel(System.Threading.Channels.Channel.CreateUnbounded<StreamingMessage>(outputOptions));
            });
        }
    }
}
