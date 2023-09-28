// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#if NETSTANDARD
using System;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Rpc
{
    /// <summary>
    /// <see cref="IServiceCollection" /> extensions for RPC.
    /// </summary>
    public static partial class RpcServiceCollectionExtensions
    {
        private static void ConfigureCallInvoker(IServiceCollection services)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<FunctionsGrpcOptions>, ConfigureOptions>());
        }

        private class ConfigureOptions : IConfigureOptions<FunctionsGrpcOptions>
        {
            private readonly IConfiguration _configuration;

            public ConfigureOptions(IConfiguration configuration)
            {
                _configuration = configuration;
            }

            public void Configure(FunctionsGrpcOptions options)
            {
                Uri address = _configuration.GetFunctionsHostGrpcUri();
                Channel c = new Channel(address.Host, address.Port, ChannelCredentials.Insecure);
                options.CallInvoker = c.CreateCallInvoker();
            }
        }
    }
}
#endif
