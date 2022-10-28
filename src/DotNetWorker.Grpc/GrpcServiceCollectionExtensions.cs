// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Channels;
using Microsoft.Azure.Functions.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Logging;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;

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

                return new GrpcHostChannel(System.Threading.Channels.Channel.CreateUnbounded<StreamingMessage>(outputOptions));
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
            services.AddSingleton<IWorkerClientFactory, GrpcWorkerClientFactory>();

            services.AddOptions<GrpcWorkerStartupOptions>()
                .Configure<IConfiguration>((arguments, config) =>
                {
                    config.Bind(arguments);
                });

            return services;
        }
    }
}
