// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#if !NETSTANDARD
using System;
using Grpc.Core;
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
            // Instead of building the GrpcChannel/CallInvoker ourselves, we use Grpc.Net.ClientFactory to
            // construct and configure the CallInvoker for us, then we attach that to our options.
            services.AddGrpcClient<CallInvokerExtractor>(_ => { })
                .ConfigureForFunctionsHostGrpc();
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<FunctionsGrpcOptions>, ConfigureOptions>());
        }

        // Used as a roundabout way of getting the configured CallInvoker from Grpc.Net.ClientFactory.
        private class CallInvokerExtractor
        {
            public CallInvokerExtractor(CallInvoker callInvoker)
            {
                CallInvoker = callInvoker ?? throw new ArgumentNullException(nameof(callInvoker));
            }

            public CallInvoker CallInvoker { get; }
        }

        private class ConfigureOptions : IConfigureOptions<FunctionsGrpcOptions>
        {
            private readonly CallInvokerExtractor _extractor;

            public ConfigureOptions(CallInvokerExtractor extractor)
            {
                _extractor = extractor;
            }

            public void Configure(FunctionsGrpcOptions options)
            {
                options.CallInvoker = _extractor.CallInvoker;
            }
        }
    }
}
#endif
