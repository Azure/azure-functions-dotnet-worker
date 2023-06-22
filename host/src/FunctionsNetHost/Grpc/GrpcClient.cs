// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Channels;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.FunctionRpc;

namespace FunctionsNetHost.Grpc
{
    internal sealed class GrpcClient
    {
        private readonly Channel<StreamingMessage> _outgoingMessageChannel;
        private readonly IncomingGrpcMessageHandler _messageHandler;
        private readonly GrpcWorkerStartupOptions _grpcWorkerStartupOptions;

        internal GrpcClient(GrpcWorkerStartupOptions grpcWorkerStartupOptions, AppLoader appLoader)
        {
            _grpcWorkerStartupOptions = grpcWorkerStartupOptions;
            var channelOptions = new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = false,
                AllowSynchronousContinuations = true
            };

            _outgoingMessageChannel = Channel.CreateUnbounded<StreamingMessage>(channelOptions);

            _messageHandler = new IncomingGrpcMessageHandler(appLoader);
        }

        internal async Task InitAsync()
        {
            var endpoint = $"http://{_grpcWorkerStartupOptions.Host}:{_grpcWorkerStartupOptions.Port}";
            Logger.LogTrace($"Grpc service endpoint:{endpoint}");

            var functionRpcClient = CreateFunctionRpcClient(endpoint);
            var eventStream = functionRpcClient.EventStream();

            await SendStartStreamMessageAsync(eventStream.RequestStream);

            var readerTask = StartReaderAsync(eventStream.ResponseStream);
            var writerTask = StartWriterAsync(eventStream.RequestStream);
            _ = StartInboundMessageForwarding();
            _ = StartOutboundMessageForwarding();

            await Task.WhenAll(readerTask, writerTask);
        }

        private async Task StartReaderAsync(IAsyncStreamReader<StreamingMessage> responseStream)
        {
            while (await responseStream.MoveNext())
            {
                await _messageHandler.ProcessMessageAsync(responseStream.Current);
            }
        }
        private async Task StartWriterAsync(IClientStreamWriter<StreamingMessage> requestStream)
        {
            await foreach (var rpcWriteMsg in _outgoingMessageChannel.Reader.ReadAllAsync())
            {
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
            await foreach (var inboundMessage in MessageChannel.Instance.InboundChannel.Reader.ReadAllAsync())
            {
                await HandleIncomingMessage(inboundMessage);
            }
        }

        /// <summary>
        /// Listens to messages in the inbound message channel and forward them the customer payload via interop layer.
        /// </summary>
        private async Task StartOutboundMessageForwarding()
        {
            await foreach (var outboundMessage in MessageChannel.Instance.OutboundChannel.Reader.ReadAllAsync())
            {
                await _outgoingMessageChannel.Writer.WriteAsync(outboundMessage);
            }
        }

        private static Task HandleIncomingMessage(StreamingMessage inboundMessage)
        {
            // Queue the work to another thread.
            Task.Run(() =>
            {
                byte[] inboundMessageBytes = inboundMessage.ToByteArray();
                NativeHostApplication.Instance.HandleInboundMessage(inboundMessageBytes, inboundMessageBytes.Length);
            });

            return Task.CompletedTask;
        }
    }
}
