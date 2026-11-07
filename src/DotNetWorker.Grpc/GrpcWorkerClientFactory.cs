// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.FunctionRpc;

namespace Microsoft.Azure.Functions.Worker.Grpc
{
    internal partial class GrpcWorkerClientFactory : IWorkerClientFactory
    {
        private readonly GrpcHostChannel _outputChannel;
        private readonly GrpcWorkerStartupOptions _startupOptions;
        private readonly ILoggerFactory _loggerFactory;

        public GrpcWorkerClientFactory(GrpcHostChannel outputChannel, IOptions<GrpcWorkerStartupOptions> startupOptions, ILoggerFactory loggerFactory)
        {
            _outputChannel = outputChannel ?? throw new ArgumentNullException(nameof(outputChannel));
            _startupOptions = startupOptions.Value ?? throw new ArgumentNullException(nameof(startupOptions), "gRPC Services are not correctly registered.");
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public IWorkerClient CreateClient(IMessageProcessor messageProcessor)
            => new GrpcWorkerClient(_outputChannel, _startupOptions, messageProcessor, _loggerFactory);

        internal partial class GrpcWorkerClient : IWorkerClient
        {
            private readonly FunctionRpcClient _grpcClient;
            private readonly GrpcWorkerStartupOptions _startupOptions;
            private readonly ChannelReader<StreamingMessage> _outputReader;
            private readonly ChannelWriter<StreamingMessage> _outputWriter;
            private readonly IMessageProcessor? _processor;
            private bool _running;

            public async Task StartAsync(CancellationToken token)
            {
                if (_running)
                {
                    throw new InvalidOperationException($"The client is already running. Multiple calls to {nameof(StartAsync)} are not supported.");
                }

                _running = true;
                await StartCoreAsync(token).ConfigureAwait(false);
            }

            private async Task StartStreamAsync(AsyncDuplexStreamingCall<StreamingMessage, StreamingMessage> eventStream)
            {
                await SendStartStreamMessageAsync(eventStream.RequestStream).ConfigureAwait(false);

                _ = StartWriterAsync(eventStream.RequestStream);
                _ = StartReaderAsync(eventStream.ResponseStream);
            }

            private async Task SendStartStreamMessageAsync(IClientStreamWriter<StreamingMessage> requestStream)
            {
                StartStream str = new StartStream()
                {
                    WorkerId = _startupOptions.WorkerId
                };

                StreamingMessage startStream = new StreamingMessage()
                {
                    StartStream = str
                };

                await requestStream.WriteAsync(startStream).ConfigureAwait(false);
            }

            public ValueTask SendMessageAsync(StreamingMessage message) => _outputWriter.WriteAsync(message);

            private async Task StartWriterAsync(IClientStreamWriter<StreamingMessage> requestStream)
            {
                await foreach (StreamingMessage rpcWriteMsg in _outputReader.ReadAllAsync().ConfigureAwait(false))
                {
                    await requestStream.WriteAsync(rpcWriteMsg).ConfigureAwait(false);
                }
            }

            private async Task StartReaderAsync(IAsyncStreamReader<StreamingMessage> responseStream)
            {
                while (await responseStream.MoveNext().ConfigureAwait(false))
                {
                    await _processor!.ProcessMessageAsync(responseStream.Current).ConfigureAwait(false);
                }
            }
        }
    }
}
