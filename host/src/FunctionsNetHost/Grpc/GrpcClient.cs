// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Channels;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.FunctionRpc;

namespace FunctionsNetHost.Grpc
{
    internal class GrpcClient
    {
        private readonly Channel<StreamingMessage> _outgoingMessageChannel;
        private readonly IncomingMessageHandler _processor;
        private readonly GrpcWorkerStartupOptions _grpcWorkerStartupOptions;

        public GrpcClient(GrpcWorkerStartupOptions grpcWorkerStartupOptions, AppLoader appLoader)
        {
            _grpcWorkerStartupOptions = grpcWorkerStartupOptions;
            var channelOptions = new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = false,
                AllowSynchronousContinuations = true
            };

            _outgoingMessageChannel = Channel.CreateUnbounded<StreamingMessage>(channelOptions);

            _processor = new IncomingMessageHandler(_outgoingMessageChannel, appLoader);
        }

        public async Task InitAsync()
        {
            var endpoint = $"http://{_grpcWorkerStartupOptions.Host}:{_grpcWorkerStartupOptions.Port}";
            Logger.Log($"Grpc server endpoint:{endpoint}");

            var client = CreateFunctionRpcClient(endpoint);

            var eventStream = client.EventStream(cancellationToken: CancellationToken.None);

            await SendStartStreamMessageAsync(eventStream.RequestStream);

            var readerTask = StartReaderAsync(eventStream.ResponseStream);
            var writerTask = StartWriterAsync(eventStream.RequestStream);
            var inboundForwardTask = StartInboundMessageForwarding();

            await Task.WhenAll(readerTask, writerTask, inboundForwardTask);

        }

        private async Task StartReaderAsync(IAsyncStreamReader<StreamingMessage> responseStream)
        {
            while (await responseStream.MoveNext())
            {
                await _processor.ProcessMessageAsync(responseStream.Current);
            }
        }
        private async Task StartWriterAsync(IClientStreamWriter<StreamingMessage> requestStream)
        {
            await foreach (var rpcWriteMsg in _outgoingMessageChannel.Reader.ReadAllAsync())
            {
                Logger.Log("Outgoing message : " + rpcWriteMsg.ContentCase);
                await requestStream.WriteAsync(rpcWriteMsg);
            }
        }

        private async Task SendStartStreamMessageAsync(IClientStreamWriter<StreamingMessage> requestStream)
        {
            var startStreamMsg = new StartStream()
            {
                WorkerId = _grpcWorkerStartupOptions.WorkerId
            };

            var startStream = new StreamingMessage()
            {
                StartStream = startStreamMsg
            };

            await requestStream.WriteAsync(startStream);
        }

        private FunctionRpcClient CreateFunctionRpcClient(string endpoint)
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var grpcUri))
            {
                throw new InvalidOperationException($"The gRPC channel URI '{endpoint}' could not be parsed.");
            }

            var grpcChannel = GrpcChannel.ForAddress(grpcUri, new GrpcChannelOptions()
            {
                MaxReceiveMessageSize = _grpcWorkerStartupOptions.GrpcMaxMessageLength,
                MaxSendMessageSize = _grpcWorkerStartupOptions.GrpcMaxMessageLength,
                Credentials = ChannelCredentials.Insecure
            });

            return new FunctionRpcClient(grpcChannel);
        }

        /// <summary>
        /// Listens to messages in the inbound message channel and forward them the customer payload via interop layer.
        /// </summary>
        private async Task StartInboundMessageForwarding()
        {
            await foreach (var inboundMessage in InboundMessageChannel.Instance.InboundChannel.Reader.ReadAllAsync())
            {
                Logger.Log($"Inbound message to customer payload: {inboundMessage.ContentCase}");

                await SendToInteropLayer(inboundMessage);
            }
        }

        private Task SendToInteropLayer(StreamingMessage inboundMessage)
        {
            // TO DO: Send to interop layer in a new Task.
            return Task.CompletedTask;
        }
    }
}
