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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Azure.Functions.Worker.Handlers;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class GrpcServiceCollectionExtensions
    {
        internal static IServiceCollection RegisterOutputChannel(this IServiceCollection services)
        {
            return services.AddSingleton<GrpcHostChannel>(s =>
            {
                UnboundedChannelOptions outputOptions = new UnboundedChannelOptions
                {
                    SingleWriter = false,
                    SingleReader = true,
                    AllowSynchronousContinuations = true
                };

                return new GrpcHostChannel(Channel.CreateUnbounded<StreamingMessage>(outputOptions));
            });
        }

        public static IServiceCollection AddGrpc(this IServiceCollection services)
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
            services.TryAddSingleton<IInvocationHandler, InvocationHandler>();

#if NET8_0_OR_GREATER
            // If we are running in the native host process, use the native client
            // for communication (interop). Otherwise; use the gRPC client.
            if (AppContext.GetData("AZURE_FUNCTIONS_NATIVE_HOST") is not null)
            {
                services.AddSingleton<IWorkerClientFactory, Azure.Functions.Worker.Grpc.NativeHostIntegration.NativeWorkerClientFactory>();
            }
            else
            {
                services.AddSingleton<IWorkerClientFactory, GrpcWorkerClientFactory>();
            }
#else
            services.AddSingleton<IWorkerClientFactory, GrpcWorkerClientFactory>();
#endif

            services.AddOptions<GrpcWorkerStartupOptions>()
                .Configure<IConfiguration>((grpcWorkerStartupOption, config) =>
                {
                    grpcWorkerStartupOption.HostEndpoint = GetFunctionsHostGrpcUri(config);
                    grpcWorkerStartupOption.RequestId = config["Functions:Worker:RequestId"];
                    grpcWorkerStartupOption.WorkerId = config["Functions:Worker:WorkerId"];
                    grpcWorkerStartupOption.GrpcMaxMessageLength = config.GetValue<int>("Functions:Worker:GrpcMaxMessageLength");
                });

            return services;
        }

        private static Uri GetFunctionsHostGrpcUri(IConfiguration configuration)
        {
            var functionsUri = configuration["Functions:Worker:HostEndpoint"];
            if (string.IsNullOrEmpty(functionsUri))
            {
                throw new InvalidOperationException("Configuration is missing the 'HostEndpoint' information. Please ensure an entry with the key 'Functions:Worker:HostEndpoint' is present in your configuration.");
            }

            if (!Uri.TryCreate(functionsUri, UriKind.Absolute, out var grpcUri))
            {
                throw new InvalidOperationException($"The gRPC channel URI '{functionsUri}' could not be parsed.");
            }

            return grpcUri;
        }
    }
}
