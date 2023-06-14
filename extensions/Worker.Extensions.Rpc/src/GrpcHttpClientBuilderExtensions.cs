// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

#if !NET6_0_OR_GREATER
using System.Net.Http;
using Grpc.Net.Client.Web;
#endif

namespace Microsoft.Azure.Functions.Worker;

/// <summary>
/// Extensions for registering RPC services to communication with the functions host.
/// </summary>
public static class GrpcHttpClientBuilderExtensions
{
    /// <summary>
    /// Configures a <see cref="IHttpClientBuilder" /> to communicate with the functions host.
    /// </summary>
    /// <param name="builder">The original builder for call chaining.</param>
    /// <remarks>
    /// When using Grpc.Net.ClientFactory 2.54.0-pre1 or earlier, use a <see cref="GrpcClientFactoryOptions"/>
    /// configure accepting overload of <c>AddGrpcClient</c>, such as
    /// <see cref="GrpcClientServiceExtensions.AddGrpcClient{TClient}(IServiceCollection, Action{GrpcClientFactoryOptions})"/>.
    /// </remarks>
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
                // We could add IOptions with explicit host URI on it,
                // but given it is a single value it is not worth the effort.
                string uriString = $"http://{config["HOST"]}:{config["PORT"]}";
                if (!Uri.TryCreate(uriString, UriKind.Absolute, out Uri? grpcUri))
                {
                    throw new InvalidOperationException($"The gRPC channel URI '{uriString}' could not be parsed.");
                }

                options.Address = grpcUri;
            });

#if !NET6_0_OR_GREATER
        // For older TFMs, we need to use Grpc.Web instead of HTTP/2
        builder.ConfigurePrimaryHttpMessageHandler(() => new GrpcWebHandler(new HttpClientHandler()));
#endif

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
                var namedOptions = service.ImplementationInstance as ConfigureNamedOptions<GrpcClientFactoryOptions>;
                if (namedOptions != null && string.Equals(builder.Name, namedOptions.Name, StringComparison.Ordinal))
                {
                    return;
                }
            }
        }

        throw new InvalidOperationException($"{caller} must be used with a gRPC client.");
    }
}
