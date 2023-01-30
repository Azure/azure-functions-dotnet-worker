// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Channels;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Logging;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GrpcServiceCollectionExtensions
    {
        internal static IServiceCollection RegisterOutputChannel(this IServiceCollection services)
        {
            return services.AddSingleton<GrpcHostChannel>(s =>
            {
                UnboundedChannelOptions outputOptions = new UnboundedChannelOptions
                {
                    SingleWriter = false,
                    SingleReader = true,
                    AllowSynchronousContinuations = true,
                };

                return new GrpcHostChannel(Channel.CreateUnbounded<StreamingMessage>(outputOptions));
            });
        }


        internal static IServiceCollection AddGrpc(this IServiceCollection services)
        {
            // Channels
            services.RegisterOutputChannel();

            // Internal logging
            services.AddSingleton<GrpcFunctionsHostLogWriter>();
            services.AddSingleton<IUserLogWriter>(p => p.GetRequiredService<GrpcFunctionsHostLogWriter>());
            services.AddSingleton<ISystemLogWriter>(p => p.GetRequiredService<GrpcFunctionsHostLogWriter>());
            services.AddSingleton<IUserMetricWriter>(p => p.GetRequiredService<GrpcFunctionsHostLogWriter>());
            services.AddSingleton<IWorkerDiagnostics, GrpcWorkerDiagnostics>();

            // FunctionMetadataProvider for worker driven function-indexing
            services.TryAddSingleton<IFunctionMetadataProvider, DefaultFunctionMetadataProvider>();

            // gRPC Core services
            services.AddSingleton<IWorker, GrpcWorker>();
            services.AddSingleton<IInvocationHandler, InvocationHandler>();

#if NET5_0_OR_GREATER
            // If we are running in the native host process, use the native client
            // for communication (interop). Otherwise; use the gRPC client.
            if (AppContext.GetData("AZURE_FUNCTIONS_NATIVE_HOST") is not null)
            {
                services.AddSingleton<IWorkerClientFactory, Azure.Functions.Worker.Grpc.NativeHostIntegration.NativeWorkerClientFactory>();
            }
            else
            {
                services.AddSingleton<IWorkerHttpClientFactory, WorkerHttpClientFactory>();
                services.AddHttpClient();
                services.AddSingleton<IWorkerClientFactory, GrpcWorkerClientFactory>();
            }
#else
            services.AddSingleton<IWorkerClientFactory, GrpcWorkerClientFactory>();
#endif

            services.AddOptions<GrpcWorkerStartupOptions>()
                .Configure<IConfiguration>((arguments, config) =>
                {
                    config.Bind(arguments);
                });

            return services;
        }

#if NET5_0_OR_GREATER
        public static IServiceCollection ConfigureGrpcClient(this IServiceCollection services, Action<IHttpClientBuilder> configure)
        {
            var builder = services.AddHttpClient(WorkerHttpClientFactory.GrpcWorkerHttClientName);
            configure(builder);
            return services;
        }
#endif
    }
}
