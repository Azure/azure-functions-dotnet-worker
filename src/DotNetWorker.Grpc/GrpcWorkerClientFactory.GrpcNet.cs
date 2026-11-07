// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#if NET6_0_OR_GREATER
using System;
using System.Threading;
using System.Threading.Tasks;
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
            private readonly GrpcChannel _grpcChannel;

            public GrpcWorkerClient(GrpcHostChannel outputChannel, GrpcWorkerStartupOptions startupOptions, IMessageProcessor processor, ILoggerFactory loggerFactory)
            {
                _startupOptions = startupOptions ?? throw new ArgumentNullException(nameof(startupOptions));
                _processor = processor ?? throw new ArgumentNullException(nameof(processor));
                _outputReader = outputChannel.Channel.Reader;
                _outputWriter = outputChannel.Channel.Writer;
                _grpcClient = CreateClient(loggerFactory, out _grpcChannel);
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
                    // The channel retries connection failures with its built-in backoff until ready or this wait is canceled.
                    // Wait before opening EventStream; channel reconnection does not recreate a failed RPC stream.
                    await _grpcChannel.ConnectAsync(token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    eventStream = _grpcClient.EventStream(cancellationToken: token);
                    await StartStreamAsync(eventStream).ConfigureAwait(false);
                }
                catch
                {
                    eventStream?.Dispose();
                    // Canceling a readiness wait removes its watcher but does not stop the channel's transport retries.
                    _grpcChannel.Dispose();
                    throw;
                }
            }

            private FunctionRpcClient CreateClient(ILoggerFactory loggerFactory, out GrpcChannel grpcChannel)
            {
                grpcChannel = GrpcChannel.ForAddress(_startupOptions.HostEndpoint!.AbsoluteUri, new()
                {
                    MaxReceiveMessageSize = _startupOptions.GrpcMaxMessageLength,
                    MaxSendMessageSize = _startupOptions.GrpcMaxMessageLength,
                    InitialReconnectBackoff = TimeSpan.FromMilliseconds(10),
                    MaxReconnectBackoff = TimeSpan.FromSeconds(10),
                    Credentials = ChannelCredentials.Insecure,
                    LoggerFactory = loggerFactory
                });

                return new FunctionRpcClient(grpcChannel);
            }
        }

    }
}
#endif
