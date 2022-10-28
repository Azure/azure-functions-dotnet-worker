// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.FunctionRpc;

#if NET5_0_OR_GREATER
using Grpc.Net.Client;
#else
using GrpcCore = Grpc.Core;
#endif

namespace Microsoft.Azure.Functions.Worker.Grpc
{
    internal class GrpcWorkerClientFactory : IWorkerClientFactory
    {
        private readonly GrpcHostChannel _outputChannel;
        private readonly GrpcWorkerStartupOptions _startupOptions;

        public GrpcWorkerClientFactory(GrpcHostChannel outputChannel, IOptions<GrpcWorkerStartupOptions> startupOptions)
        {
            _outputChannel = outputChannel ?? throw new ArgumentNullException(nameof(outputChannel));
            _startupOptions = startupOptions?.Value ?? throw new ArgumentNullException(nameof(startupOptions), "gRPC Services are not correctly registered.");
        }

        public Task<IWorkerClient> StartClientAsync(IMessageProcessor messageProcessor, CancellationToken token) 
            => GrpcWorkerClient.CreateAndStartAsync(_outputChannel, _startupOptions, messageProcessor, token);

        private class GrpcWorkerClient : IWorkerClient
        {
            private readonly FunctionRpcClient _grpcClient;
            private readonly GrpcHostChannel _outputChannel;
            private readonly GrpcWorkerStartupOptions _startupOptions;
            private readonly ChannelReader<StreamingMessage> _outputReader;
            private readonly ChannelWriter<StreamingMessage> _outputWriter;
            private bool _running;
            private IMessageProcessor? _processor;

            public GrpcWorkerClient(GrpcHostChannel outputChannel, GrpcWorkerStartupOptions startupOptions)
            {
                _outputChannel = outputChannel ?? throw new ArgumentNullException(nameof(outputChannel));
                _startupOptions = startupOptions ?? throw new ArgumentNullException(nameof(startupOptions));

                _outputReader = outputChannel.Channel.Reader;
                _outputWriter = outputChannel.Channel.Writer;

                _grpcClient = CreateClient();
            }

            internal static async Task<IWorkerClient> CreateAndStartAsync(GrpcHostChannel outputChannel, GrpcWorkerStartupOptions startupOptions, IMessageProcessor processor, CancellationToken token)
            {
                var client = new GrpcWorkerClient(outputChannel, startupOptions);
               
                await client.StartAsync(processor, token);

                return client;
            }

            private async Task StartAsync(IMessageProcessor processor, CancellationToken token)
            {
                if (_running)
                {
                    throw new InvalidOperationException($"The client is already running. Multiple calls to {nameof(StartAsync)} are not supported.");
                }
                _running = true;
                _processor = processor ?? throw new ArgumentNullException(nameof(processor));

                var eventStream = _grpcClient.EventStream(cancellationToken: token);

                await SendStartStreamMessageAsync(eventStream.RequestStream);

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

                await requestStream.WriteAsync(startStream);
            }

            public ValueTask SendMessageAsync(StreamingMessage message) => _outputWriter.WriteAsync(message);

            private async Task StartWriterAsync(IClientStreamWriter<StreamingMessage> requestStream)
            {
                await foreach (StreamingMessage rpcWriteMsg in _outputReader.ReadAllAsync())
                {
                    await requestStream.WriteAsync(rpcWriteMsg);
                }
            }

            private async Task StartReaderAsync(IAsyncStreamReader<StreamingMessage> responseStream)
            {
                while (await responseStream.MoveNext())
                {
                    await _processor!.ProcessMessageAsync(responseStream.Current);
                }
            }

            private FunctionRpcClient CreateClient()
            {
                string uriString = $"http://{_startupOptions.Host}:{_startupOptions.Port}";
                if (!Uri.TryCreate(uriString, UriKind.Absolute, out Uri? grpcUri))
                {
                    throw new InvalidOperationException($"The gRPC channel URI '{uriString}' could not be parsed.");
                }


#if NET5_0_OR_GREATER
                GrpcChannel grpcChannel = GrpcChannel.ForAddress(grpcUri, new GrpcChannelOptions()
                {
                    MaxReceiveMessageSize = _startupOptions.GrpcMaxMessageLength,
                    MaxSendMessageSize = _startupOptions.GrpcMaxMessageLength,
                    Credentials = ChannelCredentials.Insecure
                });
#else

                var options = new ChannelOption[]
                {
                    new ChannelOption(GrpcCore.ChannelOptions.MaxReceiveMessageLength, _startupOptions.GrpcMaxMessageLength),
                    new ChannelOption(GrpcCore.ChannelOptions.MaxSendMessageLength, _startupOptions.GrpcMaxMessageLength)
                };

                GrpcCore.Channel grpcChannel = new GrpcCore.Channel(_startupOptions.Host, _startupOptions.Port, ChannelCredentials.Insecure, options);

#endif
                return new FunctionRpcClient(grpcChannel);
            }
        }
    }
}
