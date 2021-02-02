// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Channels;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.WebJobs.Script.Grpc.Messages.FunctionRpc;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IFunctionsWorkerApplicationBuilder AddFunctionsWorker(this IServiceCollection services, Action<WorkerOptions> configure)
        {
            // Converters
            services.RegisterDefaultConverters();

            // Channels
            services.RegisterOutputChannel();

            // Request handling
            services.AddSingleton<IFunctionsHostClient, DefaultFunctionsHostClient>();
            services.AddSingleton<IHostRequestHandler, DefaultHostRequestHandler>();
            services.AddSingleton<IFunctionBroker, FunctionBroker>();

            // Execution
            services.AddSingleton<IFunctionInvokerFactory, DefaultFunctionInvokerFactory>();
            services.AddSingleton<IMethodInvokerFactory, DefaultMethodInvokerFactory>();
            services.AddSingleton<IFunctionActivator, DefaultFunctionActivator>();
            services.AddSingleton<IFunctionExecutor, DefaultFunctionExecutor>();

            // Function Execution Contexts
            services.AddSingleton<IFunctionExecutionContextFactory, DefaultFunctionExecutionContextFactory>();

            // Function Definition
            services.AddSingleton<IFunctionDefinitionFactory, DefaultFunctionDefinitionFactory>();

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

            return new FunctionsWorkerApplicationBuilder(services);
        }

        internal static IServiceCollection RegisterDefaultConverters(this IServiceCollection services)
        {
            return services.AddSingleton<IConverter, OutputBindingConverter>()
                           .AddSingleton<IConverter, ExactMatchConverter>()
                           .AddSingleton<IConverter, JsonPocoConverter>();
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
