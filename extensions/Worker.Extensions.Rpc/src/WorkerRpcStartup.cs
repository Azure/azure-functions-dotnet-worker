// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Rpc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

#if NETSTANDARD
using Microsoft.Extensions.Configuration;
#endif

[assembly: WorkerExtensionStartup(typeof(WorkerRpcStartup))]

namespace Microsoft.Azure.Functions.Worker.Extensions.Rpc
{

    /// <summary>
    /// Startup code for extensions RPC.
    /// </summary>
    public sealed class WorkerRpcStartup : WorkerExtensionStartup
    {
        /// <inheritdoc/>
        public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
        {
            if (applicationBuilder is null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            OptionsBuilder<FunctionsGrpcOptions> builder = applicationBuilder.Services
                .AddOptions<FunctionsGrpcOptions>();

            ConfigureCallInvoker(builder);
            builder.Validate(options => options.CallInvoker is not null, "gRPC CallInvoker must not be null.");
        }

        private void ConfigureCallInvoker(OptionsBuilder<FunctionsGrpcOptions> builder)
        {
#if NETSTANDARD
            builder.Configure<IConfiguration>((options, config) =>
            {
                Uri address = config.GetFunctionsHostGrpcUri();
                Channel c = new Channel(address.Host, address.Port, ChannelCredentials.Insecure);
                options.CallInvoker = c.CreateCallInvoker();
            });

#else
            // Instead of building the GrpcChannel/CallInvoker ourselves, we use Grpc.Net.ClientFactory to
            // construct and configure the CallInvoker for us, then we attach that to our options.
            builder.Services.AddGrpcClient<CallInvokerExtractor>(_ => { })
                .ConfigureForFunctionsHostGrpc();

            builder.Configure<CallInvokerExtractor>((options, extractor) =>
            {
                options.CallInvoker = extractor.CallInvoker;
            });
#endif
        }

#if !NETSTANDARD
        // Used as a roundabout way of getting the configured CallInvoker from Grpc.Net.ClientFactory.
        private class CallInvokerExtractor
        {
            public CallInvokerExtractor(CallInvoker callInvoker)
            {
                CallInvoker = callInvoker ?? throw new ArgumentNullException(nameof(callInvoker));
            }

            public CallInvoker CallInvoker { get; }
        }
#endif
    }
}
