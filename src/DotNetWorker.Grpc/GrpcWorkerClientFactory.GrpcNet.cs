// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#if NET6_0_OR_GREATER
using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.FunctionRpc;

namespace Microsoft.Azure.Functions.Worker.Grpc
{
    internal partial class GrpcWorkerClientFactory
    {
        internal partial class GrpcWorkerClient
        {
            private readonly GrpcChannel _grpcChannel;

            public GrpcWorkerClient(GrpcHostChannel outputChannel, GrpcWorkerStartupOptions startupOptions, IMessageProcessor processor)
            {
                _startupOptions = startupOptions ?? throw new ArgumentNullException(nameof(startupOptions));
                _processor = processor ?? throw new ArgumentNullException(nameof(processor));
                _outputReader = outputChannel.Channel.Reader;
                _outputWriter = outputChannel.Channel.Writer;
                _grpcClient = CreateClient(out _grpcChannel);
            }

            internal GrpcWorkerClient(GrpcHostChannel outputChannel, GrpcWorkerStartupOptions startupOptions, IMessageProcessor processor,
                GrpcChannel grpcChannel, FunctionRpcClient grpcClient)
            {
                _startupOptions = startupOptions ?? throw new ArgumentNullException(nameof(startupOptions));
                _processor = processor ?? throw new ArgumentNullException(nameof(processor));
                _outputReader = outputChannel.Channel.Reader;
                _outputWriter = outputChannel.Channel.Writer;
                _grpcChannel = grpcChannel;
                _grpcClient = grpcClient;
            }

            private async Task StartCoreAsync(CancellationToken token)
            {
                AsyncDuplexStreamingCall<StreamingMessage, StreamingMessage>? eventStream = null;
                try
                {
                    token.ThrowIfCancellationRequested();
                    // Wait-for-ready keeps initial connection retries inside the RPC, using the channel's backoff.
                    // It does not recreate or replay an established stream.
                    eventStream = _grpcClient.EventStream(new CallOptions(cancellationToken: token).WithWaitForReady());
                    await StartStreamAsync(eventStream).ConfigureAwait(false);
                }
                catch
                {
                    eventStream?.Dispose();
                    // Stop transport retries if startup fails or is canceled.
                    _grpcChannel.Dispose();
                    throw;
                }
            }

            private FunctionRpcClient CreateClient(out GrpcChannel grpcChannel)
            {
                grpcChannel = GrpcChannel.ForAddress(_startupOptions.HostEndpoint!.AbsoluteUri, new()
                {
                    MaxReceiveMessageSize = _startupOptions.GrpcMaxMessageLength,
                    MaxSendMessageSize = _startupOptions.GrpcMaxMessageLength,
                    InitialReconnectBackoff = TimeSpan.FromMilliseconds(10),
                    MaxReconnectBackoff = TimeSpan.FromSeconds(10),
                    Credentials = ChannelCredentials.Insecure
                });

                return new FunctionRpcClient(grpcChannel);
            }
        }

    }
}
#endif
