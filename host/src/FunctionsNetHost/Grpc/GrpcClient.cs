// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Channels;
using Google.Protobuf;
using GrpcCore = Grpc.Core;
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
        private readonly Func<IGrpcEventStream> _createEventStream;
        private readonly Func<TimeSpan, Task> _delayAsync;

        internal GrpcClient(
            GrpcWorkerStartupOptions grpcWorkerStartupOptions,
            AppLoader appLoader,
            Func<IGrpcEventStream>? createEventStream = null,
            Func<TimeSpan, Task>? delayAsync = null)
        {
            _grpcWorkerStartupOptions = grpcWorkerStartupOptions;
            var channelOptions = new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = false,
                AllowSynchronousContinuations = true
            };

            _outgoingMessageChannel = Channel.CreateUnbounded<StreamingMessage>(channelOptions);

            _messageHandler = new IncomingGrpcMessageHandler(appLoader, _grpcWorkerStartupOptions);
            _createEventStream = createEventStream ?? CreateEventStream;
            _delayAsync = delayAsync ?? Task.Delay;
        }

        internal async Task InitAsync()
        {
            using var eventStream = await ConnectAsync();

            var readerTask = StartReaderAsync(eventStream.ResponseStream);
            var writerTask = StartWriterAsync(eventStream.RequestStream);
            _ = StartInboundMessageForwarding();
            _ = StartOutboundMessageForwarding();

            await Task.WhenAll(readerTask, writerTask);
        }

        internal async Task<IGrpcEventStream> ConnectAsync()
        {
            var endpoint = _grpcWorkerStartupOptions.ServerUri.AbsoluteUri;
            Logger.LogTrace($"Grpc service endpoint:{endpoint}");

            var stopwatch = Stopwatch.StartNew();
            var attempt = 0;

            while (true)
            {
                attempt++;
                IGrpcEventStream? eventStream = null;

                try
                {
                    eventStream = _createEventStream();
                    await SendStartStreamMessageAsync(eventStream.RequestStream);
                    return eventStream;
                }
                catch (Exception ex) when (TryGetRetryDelay(ex, stopwatch.Elapsed, out var retryDelay))
                {
                    eventStream?.Dispose();
                    Logger.Log($"Initial gRPC connection attempt {attempt} to '{endpoint}' failed with transient error '{ex.GetType().Name}: {ex.Message}'. Retrying in {retryDelay.TotalMilliseconds:0} ms.");
                    await _delayAsync(retryDelay);
                }
                catch (Exception ex) when (IsTransientStartupConnectionFailure(ex))
                {
                    eventStream?.Dispose();

                    var message = $"Failed to establish initial gRPC connection to '{endpoint}' after {attempt} attempts over {stopwatch.Elapsed:c}.";
                    Logger.Log($"{message} Last error: {ex.GetType().Name}: {ex.Message}");

                    throw new InvalidOperationException(message, ex);
                }
                catch
                {
                    eventStream?.Dispose();
                    throw;
                }
            }
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

        private bool TryGetRetryDelay(Exception exception, TimeSpan elapsed, out TimeSpan retryDelay)
        {
            retryDelay = default;

            if (!IsTransientStartupConnectionFailure(exception))
            {
                return false;
            }

            var remainingRetryWindow = _grpcWorkerStartupOptions.InitialConnectionRetryTimeout - elapsed;
            if (remainingRetryWindow <= TimeSpan.Zero)
            {
                return false;
            }

            retryDelay = remainingRetryWindow < _grpcWorkerStartupOptions.InitialConnectionRetryDelay
                ? remainingRetryWindow
                : _grpcWorkerStartupOptions.InitialConnectionRetryDelay;

            return true;
        }

        internal static bool IsTransientStartupConnectionFailure(Exception? exception)
        {
            return exception switch
            {
                null => false,
                HttpRequestException => true,
                IOException => true,
                SocketException => true,
                GrpcCore.RpcException rpcException => rpcException.StatusCode == StatusCode.Unavailable
                    || rpcException.StatusCode == StatusCode.DeadlineExceeded
                    || IsTransientStartupConnectionFailure(rpcException.InnerException),
                _ => IsTransientStartupConnectionFailure(exception.InnerException)
            };
        }

        private IGrpcEventStream CreateEventStream()
        {
            var endpoint = _grpcWorkerStartupOptions.ServerUri.AbsoluteUri;
            var grpcChannel = GrpcChannel.ForAddress(endpoint, new GrpcChannelOptions()
            {
                MaxReceiveMessageSize = _grpcWorkerStartupOptions.GrpcMaxMessageLength,
                MaxSendMessageSize = _grpcWorkerStartupOptions.GrpcMaxMessageLength,
                Credentials = ChannelCredentials.Insecure
            });

            var functionRpcClient = new FunctionRpcClient(grpcChannel);
            return new FunctionRpcEventStream(grpcChannel, functionRpcClient.EventStream());
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

        private sealed class FunctionRpcEventStream(
            GrpcChannel grpcChannel,
            AsyncDuplexStreamingCall<StreamingMessage, StreamingMessage> eventStream) : IGrpcEventStream
        {
            public IAsyncStreamReader<StreamingMessage> ResponseStream { get; } = eventStream.ResponseStream;

            public IClientStreamWriter<StreamingMessage> RequestStream { get; } = eventStream.RequestStream;

            public void Dispose()
            {
                eventStream.Dispose();
                grpcChannel.Dispose();
            }
        }
    }
}
