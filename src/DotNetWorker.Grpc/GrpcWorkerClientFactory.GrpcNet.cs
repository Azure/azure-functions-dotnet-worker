// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#if NET6_0_OR_GREATER
using System;
using System.Threading;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Logging;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.FunctionRpc;

namespace Microsoft.Azure.Functions.Worker.Grpc
{
    internal partial class GrpcWorkerClientFactory
    {
        internal partial class GrpcWorkerClient
        {
            private readonly GrpcChannel? _grpcChannel;

            public GrpcWorkerClient(GrpcHostChannel outputChannel, GrpcWorkerStartupOptions startupOptions, IMessageProcessor processor,
                ILogger<GrpcWorkerClientFactory> logger)
                : this(outputChannel, startupOptions, processor, logger, CreateChannel(startupOptions))
            {
            }

            internal GrpcWorkerClient(GrpcHostChannel outputChannel, GrpcWorkerStartupOptions startupOptions, IMessageProcessor processor,
                ILogger<GrpcWorkerClientFactory> logger, GrpcChannel grpcChannel, FunctionRpcClient? grpcClient = null)
                : this(outputChannel, startupOptions, processor, logger,
                    grpcClient ?? new FunctionRpcClient(grpcChannel ?? throw new ArgumentNullException(nameof(grpcChannel))))
            {
                _grpcChannel = grpcChannel ?? throw new ArgumentNullException(nameof(grpcChannel));
            }

            private AsyncDuplexStreamingCall<StreamingMessage, StreamingMessage> CreateEventStream(CancellationToken token)
            {
                // Wait-for-ready keeps initial connection retries inside the RPC, using the channel's backoff.
                // It does not recreate or replay an established stream.
                return _grpcClient.EventStream(new CallOptions(cancellationToken: token).WithWaitForReady());
            }

            private static GrpcChannel CreateChannel(GrpcWorkerStartupOptions startupOptions)
            {
                if (startupOptions is null)
                {
                    throw new ArgumentNullException(nameof(startupOptions));
                }

                return GrpcChannel.ForAddress(startupOptions.HostEndpoint!.AbsoluteUri, new()
                {
                    MaxReceiveMessageSize = startupOptions.GrpcMaxMessageLength,
                    MaxSendMessageSize = startupOptions.GrpcMaxMessageLength,
                    InitialReconnectBackoff = TimeSpan.FromMilliseconds(10),
                    MaxReconnectBackoff = TimeSpan.FromSeconds(10),
                    Credentials = ChannelCredentials.Insecure
                });
            }
        }
    }
}
#endif
