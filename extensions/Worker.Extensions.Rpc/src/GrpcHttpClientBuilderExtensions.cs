// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#if !NETSTANDARD

using System;
using System.Runtime.CompilerServices;
using Grpc.Net.ClientFactory;
using Microsoft.Azure.Functions.Worker.Extensions.Rpc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Extensions for registering RPC services to communicate with the functions host.
    /// </summary>
    public static class GrpcHttpClientBuilderExtensions
    {
        /// <summary>
        /// Configures a <see cref="IHttpClientBuilder" /> to communicate with the functions host.
        /// </summary>
        /// <param name="builder">The original builder for call chaining.</param>
        public static IHttpClientBuilder ConfigureForFunctionsHostGrpc(this IHttpClientBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            ValidateGrpcClient(builder);
            builder.Services.AddOptions<GrpcClientFactoryOptions>(builder.Name)
                .Configure<IConfiguration>((options, config) =>
                {
                    options.Address = config.GetFunctionsHostGrpcUri();
                    if (config.GetFunctionsHostMaxMessageLength() is int length)
                    {
                        options.ChannelOptionsActions.Add(o =>
                        {
                            o.MaxReceiveMessageSize = length;
                            o.MaxSendMessageSize = length;
                        });
                    }
                });

            return builder;
        }

        // Validates the IHttpClientBuilder is from a IServiceCollection.AddGrpcClient call, and not some other IHttpClientBuilder.
        // Code taken from https://github.com/grpc/grpc-dotnet/blob/ff1a07b90c498f259e6d9f4a50cdad7c89ecd3c0/src/Grpc.Net.ClientFactory/GrpcHttpClientBuilderExtensions.cs#L382.
        private static void ValidateGrpcClient(IHttpClientBuilder builder, [CallerMemberName] string? caller = null)
        {
            // Validate the builder is for a gRPC client
            foreach (var service in builder.Services)
            {
                if (service.ServiceType == typeof(IConfigureOptions<GrpcClientFactoryOptions>))
                {
                    // Builder is from AddGrpcClient if options have been configured with the same name
                    if (service.ImplementationInstance is ConfigureNamedOptions<GrpcClientFactoryOptions> namedOptions
                        && string.Equals(builder.Name, namedOptions.Name, StringComparison.Ordinal))
                    {
                        return;
                    }
                }
            }

            throw new InvalidOperationException($"{caller} must be used with a gRPC client.");
        }
    }
}
#endif
