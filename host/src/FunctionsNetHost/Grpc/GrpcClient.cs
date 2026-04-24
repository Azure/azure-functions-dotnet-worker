// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net.Sockets;
using System.Threading.Channels;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.FunctionRpc;
using GrpcCore = Grpc.Core;

namespace FunctionsNetHost.Grpc
{
    internal sealed class GrpcClient
    {
        private static readonly TimeSpan RetryProgressLogInterval = TimeSpan.FromSeconds(10);

        private readonly Channel<StreamingMessage> _outgoingMessageChannel;
        private readonly IncomingGrpcMessageHandler _messageHandler;
        private readonly GrpcWorkerStartupOptions _grpcWorkerStartupOptions;
        private readonly Func<IGrpcEventStream> _createEventStream;
        private readonly Func<TimeSpan, Task> _delayAsync;
        private readonly Action<string> _log;
        private readonly Func<DateTimeOffset> _getUtcNow;

        internal GrpcClient(
            GrpcWorkerStartupOptions grpcWorkerStartupOptions,
            AppLoader appLoader,
            Func<IGrpcEventStream>? createEventStream = null,
            Func<TimeSpan, Task>? delayAsync = null,
            Action<string>? log = null,
            Func<DateTimeOffset>? getUtcNow = null)
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
            _log = log ?? Logger.Log;
            _getUtcNow = getUtcNow ?? (() => DateTimeOffset.UtcNow);
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

            var connectStartTime = _getUtcNow();
            var attempt = 0;
            var totalRetryCount = 0;
            RetryLogState? retryLogState = null;

            while (true)
            {
                attempt++;
                IGrpcEventStream? eventStream = null;

                try
                {
                    eventStream = _createEventStream();
                    await SendStartStreamMessageAsync(eventStream.RequestStream);
                    LogConnectionSuccess(endpoint, totalRetryCount, GetElapsed(connectStartTime));
                    return eventStream;
                }
                catch (Exception ex) when (TryGetRetryDelay(ex, GetElapsed(connectStartTime), out var retryDelay))
                {
                    eventStream?.Dispose();
                    totalRetryCount++;
                    TrackRetryProgress(endpoint, ex, GetElapsed(connectStartTime), _getUtcNow(), ref retryLogState);
                    await _delayAsync(retryDelay);
                }
                catch (Exception ex) when (IsTransientStartupConnectionFailure(ex))
                {
                    eventStream?.Dispose();

                    var message = $"Failed to establish initial gRPC connection to '{endpoint}' after {attempt} attempts over {GetElapsed(connectStartTime):c}.";
                    _log($"{message} Last error: {ex.GetType().Name}: {ex.Message}");

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

        private void TrackRetryProgress(string endpoint, Exception exception, TimeSpan elapsed, DateTimeOffset currentTime, ref RetryLogState? retryLogState)
        {
            var failureSignature = GetFailureSignature(exception);

            if (retryLogState is not null && !retryLogState.HasSameFailureSignature(failureSignature))
            {
                LogRetryProgress(endpoint, elapsed, retryLogState);
                retryLogState = null;
            }

            if (retryLogState is null)
            {
                retryLogState = new RetryLogState(failureSignature, currentTime + RetryProgressLogInterval);
                return;
            }

            retryLogState.RegisterRetry();

            if (!retryLogState.ShouldLogProgress(currentTime))
            {
                return;
            }

            LogRetryProgress(endpoint, elapsed, retryLogState);
            retryLogState.MarkProgressLogged(currentTime + RetryProgressLogInterval);
        }

        private void LogConnectionSuccess(string endpoint, int totalRetryCount, TimeSpan elapsed)
        {
            if (totalRetryCount == 0)
            {
                return;
            }

            _log($"Initial gRPC connection to '{endpoint}' succeeded after {totalRetryCount} retries over {elapsed:c}.");
        }

        private void LogRetryProgress(string endpoint, TimeSpan elapsed, RetryLogState retryLogState)
        {
            if (!retryLogState.HasUnloggedRepeatedFailures)
            {
                return;
            }

            _log($"Initial gRPC connection to '{endpoint}' is still retrying. Retried {retryLogState.RetryCount} times over {elapsed:c} with repeated error '{retryLogState.FailureSignature}'.");
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

        private TimeSpan GetElapsed(DateTimeOffset connectStartTime)
        {
            var elapsed = _getUtcNow() - connectStartTime;
            return elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
        }

        private static string GetFailureSignature(Exception exception)
        {
            return $"{exception.GetType().Name}: {exception.Message}";
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

        private sealed class RetryLogState(string failureSignature, DateTimeOffset nextProgressLogTime)
        {
            public string FailureSignature { get; } = failureSignature;

            public int RetryCount { get; private set; } = 1;

            public int LoggedRetryCount { get; private set; }

            public DateTimeOffset NextProgressLogTime { get; private set; } = nextProgressLogTime;

            public bool HasUnloggedRepeatedFailures => RetryCount > 1 && RetryCount > LoggedRetryCount;

            public bool HasSameFailureSignature(string failureSignature)
            {
                return string.Equals(FailureSignature, failureSignature, StringComparison.Ordinal);
            }

            public void RegisterRetry()
            {
                RetryCount++;
            }

            public bool ShouldLogProgress(DateTimeOffset currentTime)
            {
                return HasUnloggedRepeatedFailures && currentTime >= NextProgressLogTime;
            }

            public void MarkProgressLogged(DateTimeOffset nextProgressLogTime)
            {
                LoggedRetryCount = RetryCount;
                NextProgressLogTime = nextProgressLogTime;
            }
        }
    }
}
