// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Logging;
using RpcException = Grpc.Core.RpcException;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.FunctionRpc;

#if NET6_0_OR_GREATER
using Grpc.Net.Client;
#else
using GrpcCore = Grpc.Core;
#endif

namespace Microsoft.Azure.Functions.Worker.Grpc
{
    internal partial class GrpcWorkerClientFactory : IWorkerClientFactory
    {
        private readonly GrpcHostChannel _outputChannel;
        private readonly GrpcWorkerStartupOptions _startupOptions;
        private readonly ILogger<GrpcWorkerClientFactory> _logger;

        public GrpcWorkerClientFactory(GrpcHostChannel outputChannel, IOptions<GrpcWorkerStartupOptions> startupOptions, ILogger<GrpcWorkerClientFactory> logger)
        {
            _outputChannel = outputChannel ?? throw new ArgumentNullException(nameof(outputChannel));
            _startupOptions = startupOptions.Value ?? throw new ArgumentNullException(nameof(startupOptions), "gRPC Services are not correctly registered.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IWorkerClient CreateClient(IMessageProcessor messageProcessor)
            => new GrpcWorkerClient(_outputChannel, _startupOptions, messageProcessor, _logger);

        internal sealed class GrpcWorkerClient : IWorkerClient
        {
            private readonly FunctionRpcClient _grpcClient;
            private readonly GrpcWorkerStartupOptions _startupOptions;
            private readonly ChannelReader<StreamingMessage> _outputReader;
            private readonly ChannelWriter<StreamingMessage> _outputWriter;
            private readonly IMessageProcessor? _processor;
            private readonly ILogger<GrpcWorkerClientFactory> _logger;
            private readonly SemaphoreSlim _writeLock = new(1, 1);
            private readonly CancellationTokenSource _connectionClosed = new();
            private CancellationTokenRegistration _startupRegistration;
            private AsyncDuplexStreamingCall<StreamingMessage, StreamingMessage>? _eventStream;
            private IClientStreamWriter<StreamingMessage>? _requestStream;
            private Task? _writerTask;
            private Task? _readerTask;
            private bool _running;

            public GrpcWorkerClient(GrpcHostChannel outputChannel, GrpcWorkerStartupOptions startupOptions, IMessageProcessor processor, ILogger<GrpcWorkerClientFactory> logger)
                : this(outputChannel, startupOptions, processor, logger, CreateClient(startupOptions ?? throw new ArgumentNullException(nameof(startupOptions))))
            {
            }

            internal GrpcWorkerClient(GrpcHostChannel outputChannel, GrpcWorkerStartupOptions startupOptions, IMessageProcessor processor,
                ILogger<GrpcWorkerClientFactory> logger, FunctionRpcClient grpcClient)
            {
                _startupOptions = startupOptions ?? throw new ArgumentNullException(nameof(startupOptions));
                _processor = processor ?? throw new ArgumentNullException(nameof(processor));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));

                _outputReader = outputChannel.Channel.Reader;
                _outputWriter = outputChannel.Channel.Writer;

                _grpcClient = grpcClient ?? throw new ArgumentNullException(nameof(grpcClient));
            }

            public async Task StartAsync(CancellationToken token)
            {
                if (_running)
                {
                    throw new InvalidOperationException($"The client is already running. Multiple calls to {nameof(StartAsync)} are not supported.");
                }

                _running = true;

                _startupRegistration = token.Register(static state => ((CancellationTokenSource)state!).Cancel(), _connectionClosed);
                try
                {
                    _eventStream = _grpcClient.EventStream(cancellationToken: _connectionClosed.Token);
                    await SendStartStreamMessageAsync(_eventStream.RequestStream).ConfigureAwait(false);
                    _requestStream = _eventStream.RequestStream;
                    _writerTask = ObserveConnectionAsync(StartWriterAsync(_eventStream.RequestStream));
                    _readerTask = ObserveConnectionAsync(StartReaderAsync(_eventStream.ResponseStream));
                }
                catch
                {
                    _connectionClosed.Cancel();
                    _eventStream?.Dispose();
                    _startupRegistration.Dispose();
                    throw;
                }
            }

            public bool SupportsExtensionPayload => true;

            public CancellationToken ConnectionClosed => _connectionClosed.Token;

            public async Task StopAsync(CancellationToken cancellationToken)
            {
                _connectionClosed.Cancel();
                _eventStream?.Dispose();
                await Task.WhenAll(_writerTask ?? Task.CompletedTask, _readerTask ?? Task.CompletedTask).ConfigureAwait(false);
                _startupRegistration.Dispose();
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

            public async ValueTask SendPayloadMessageAsync(StreamingMessage message, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _connectionClosed.Token.ThrowIfCancellationRequested();
                IClientStreamWriter<StreamingMessage> stream = _requestStream
                    ?? throw new InvalidOperationException("The worker gRPC stream has not started.");
                if (_startupOptions.GrpcMaxMessageLength > 0 && message.CalculateSize() > _startupOptions.GrpcMaxMessageLength)
                {
                    throw new InvalidOperationException("The extension payload message exceeds the configured gRPC message limit.");
                }

                using IDisposable guard = await _writeLock.LockAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                _connectionClosed.Token.ThrowIfCancellationRequested();
                await stream.WriteAsync(message).ConfigureAwait(false);
            }

            private async Task StartWriterAsync(IClientStreamWriter<StreamingMessage> requestStream)
            {
                _requestStream = requestStream;
                await foreach (StreamingMessage rpcWriteMsg in _outputReader.ReadAllAsync(_connectionClosed.Token).ConfigureAwait(false))
                {
                    using IDisposable guard = await _writeLock.LockAsync(_connectionClosed.Token).ConfigureAwait(false);
                    await requestStream.WriteAsync(rpcWriteMsg).ConfigureAwait(false);
                }
            }

            private async Task StartReaderAsync(IAsyncStreamReader<StreamingMessage> responseStream)
            {
                while (await responseStream.MoveNext(_connectionClosed.Token).ConfigureAwait(false))
                {
                    await _processor!.ProcessMessageAsync(responseStream.Current).ConfigureAwait(false);
                }
            }

            private async Task ObserveConnectionAsync(Task loop)
            {
                try
                {
                    await loop.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_connectionClosed.IsCancellationRequested)
                {
                }
                catch (RpcException exception) when (_connectionClosed.IsCancellationRequested && exception.StatusCode == StatusCode.Cancelled)
                {
                }
                catch (Exception exception)
                {
                    LogConnectionFailure(_logger, exception);
                }
                finally
                {
                    _connectionClosed.Cancel();
                    _outputWriter.TryComplete();
                    _eventStream?.Dispose();
                }
            }

            private static FunctionRpcClient CreateClient(GrpcWorkerStartupOptions startupOptions)
            {
#if NET6_0_OR_GREATER
                GrpcChannel grpcChannel = GrpcChannel.ForAddress(startupOptions.HostEndpoint!.AbsoluteUri, new GrpcChannelOptions()
                {
                    MaxReceiveMessageSize = startupOptions.GrpcMaxMessageLength,
                    MaxSendMessageSize = startupOptions.GrpcMaxMessageLength,
                    Credentials = ChannelCredentials.Insecure
                });
#else

                var options = new ChannelOption[]
                {
                    new ChannelOption(GrpcCore.ChannelOptions.MaxReceiveMessageLength, startupOptions.GrpcMaxMessageLength),
                    new ChannelOption(GrpcCore.ChannelOptions.MaxSendMessageLength, startupOptions.GrpcMaxMessageLength)
                };

                GrpcCore.Channel grpcChannel = new GrpcCore.Channel(startupOptions.HostEndpoint!.Host, startupOptions.HostEndpoint.Port, ChannelCredentials.Insecure, options);

#endif
                return new FunctionRpcClient(grpcChannel);
            }
        }

        [LoggerMessage(1, LogLevel.Error, "The worker gRPC connection failed.")]
        private static partial void LogConnectionFailure(ILogger logger, Exception exception);
    }
}
