// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#if !NET6_0_OR_GREATER
using System;
using System.Threading;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Logging;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.FunctionRpc;
using GrpcCore = Grpc.Core;

namespace Microsoft.Azure.Functions.Worker.Grpc
{
    internal partial class GrpcWorkerClientFactory
    {
        internal partial class GrpcWorkerClient
        {
            public GrpcWorkerClient(GrpcHostChannel outputChannel, GrpcWorkerStartupOptions startupOptions, IMessageProcessor processor,
                ILogger<GrpcWorkerClientFactory> logger)
                : this(outputChannel, startupOptions, processor, logger, CreateClient(startupOptions))
            {
            }

            private AsyncDuplexStreamingCall<StreamingMessage, StreamingMessage> CreateEventStream(CancellationToken token)
                => _grpcClient.EventStream(cancellationToken: token);

            private static FunctionRpcClient CreateClient(GrpcWorkerStartupOptions startupOptions)
            {
                if (startupOptions is null)
                {
                    throw new ArgumentNullException(nameof(startupOptions));
                }

                var options = new ChannelOption[]
                {
                    new ChannelOption(GrpcCore.ChannelOptions.MaxReceiveMessageLength, startupOptions.GrpcMaxMessageLength),
                    new ChannelOption(GrpcCore.ChannelOptions.MaxSendMessageLength, startupOptions.GrpcMaxMessageLength)
                };

                GrpcCore.Channel grpcChannel = new GrpcCore.Channel(startupOptions.HostEndpoint!.Host, startupOptions.HostEndpoint.Port, ChannelCredentials.Insecure, options);

                return new FunctionRpcClient(grpcChannel);
            }
        }
    }
}
#endif
