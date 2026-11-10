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

        internal sealed partial class GrpcWorkerClient : IWorkerClient
        {
            private readonly FunctionRpcClient _grpcClient;
            private readonly GrpcWorkerStartupOptions _startupOptions;
            private readonly ChannelReader<StreamingMessage> _outputReader;
            private readonly ChannelWriter<StreamingMessage> _outputWriter;
            private readonly IMessageProcessor? _processor;
            private readonly ILogger<GrpcWorkerClientFactory> _logger;
            private readonly SemaphoreSlim _writeLock = new(1, 1);
            private readonly CancellationTokenSource _connectionClosed = new();
#if NET9_0_OR_GREATER
            private readonly Lock _lifecycleLock = new();
#else
            private readonly object _lifecycleLock = new();
#endif
            private readonly TaskCompletionSource<bool> _startupCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource<bool> _terminationCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private AsyncDuplexStreamingCall<StreamingMessage, StreamingMessage>? _eventStream;
            private IClientStreamWriter<StreamingMessage>? _requestStream;
            private Task _completion = Task.CompletedTask;
            private bool _started;
            private bool _terminated;

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
                BeginStartup();

                CancellationTokenRegistration startupRegistration = default;
                Task writerTask = Task.CompletedTask;
                Task readerTask = Task.CompletedTask;
                Task startupCleanupTask = Task.CompletedTask;
                try
                {
                    startupRegistration = token.Register(static state => ((GrpcWorkerClient)state!).TerminateConnection(), this);
                    _connectionClosed.Token.ThrowIfCancellationRequested();
                    var eventStream = CreateEventStream(_connectionClosed.Token);
                    if (!TryPublishCall(eventStream))
                    {
                        // Startup still owns a call that was created after termination began.
                        try
                        {
                            eventStream.Dispose();
                        }
                        catch (Exception exception)
                        {
                            startupCleanupTask = Task.FromException(exception);
                            throw;
                        }

                        throw new OperationCanceledException(_connectionClosed.Token);
                    }

                    await SendStartStreamMessageAsync(eventStream.RequestStream).ConfigureAwait(false);
                    _requestStream = eventStream.RequestStream;
                    writerTask = ObserveLoopAndTerminateConnectionAsync(StartWriterAsync(eventStream.RequestStream));
                    readerTask = ObserveLoopAndTerminateConnectionAsync(StartReaderAsync(eventStream.ResponseStream));
                }
                catch
                {
                    TerminateConnection();
                    throw;
                }
                finally
                {
                    _completion = DrainConnectionAsync(writerTask, readerTask, startupCleanupTask, startupRegistration);
                    _startupCompleted.TrySetResult(true);
                }
            }

            private void BeginStartup()
            {
                lock (_lifecycleLock)
                {
                    if (_started || _terminated)
                    {
                        throw new InvalidOperationException($"The client cannot be started again. Multiple calls to {nameof(StartAsync)} are not supported.");
                    }

                    _started = true;
                }
            }

            private bool TryPublishCall(AsyncDuplexStreamingCall<StreamingMessage, StreamingMessage> eventStream)
            {
                lock (_lifecycleLock)
                {
                    if (_terminated)
                    {
                        return false;
                    }

                    _eventStream = eventStream;
                    return true;
                }
            }

            public bool SupportsExtensionPayload =>
#if NET7_0_OR_GREATER
                true;
#else
                false;
#endif

            public CancellationToken ConnectionClosed => _connectionClosed.Token;

            public async Task StopAsync(CancellationToken cancellationToken)
            {
                TerminateConnection();
                await _startupCompleted.Task.ConfigureAwait(false);
                await _completion.ConfigureAwait(false);
                await _terminationCompleted.Task.ConfigureAwait(false);
            }

            private void TerminateConnection()
            {
                AsyncDuplexStreamingCall<StreamingMessage, StreamingMessage>? eventStream;
                bool started;
                lock (_lifecycleLock)
                {
                    if (_terminated)
                    {
                        return;
                    }

                    _terminated = true;
                    started = _started;
                    eventStream = _eventStream;
                    _eventStream = null;
                }

                _outputWriter.TryComplete();
                Exception? failure = null;
                try
                {
                    _connectionClosed.Cancel();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }

                try
                {
                    eventStream?.Dispose();
                }
                catch (Exception exception)
                {
                    failure = failure is null ? exception : new AggregateException(failure, exception);
                }

#if NET6_0_OR_GREATER
                try
                {
                    // Stop channel retries even when startup has not published an RPC call.
                    _grpcChannel?.Dispose();
                }
                catch (Exception exception)
                {
                    failure = failure is null ? exception : new AggregateException(failure, exception);
                }
#endif

                if (failure is null)
                {
                    _terminationCompleted.TrySetResult(true);
                }
                else
                {
                    _terminationCompleted.TrySetException(failure);
                    LogConnectionFailure(_logger, failure);
                }

                if (!started)
                {
                    _startupCompleted.TrySetResult(true);
                }
            }

            private async Task DrainConnectionAsync(Task writerTask, Task readerTask, Task startupCleanupTask,
                CancellationTokenRegistration startupRegistration)
            {
                Task drain = Task.WhenAll(writerTask, readerTask, startupCleanupTask, _terminationCompleted.Task);
                try
                {
                    // Finish cancellation callbacks before releasing the registration that may have invoked them.
                    await drain.ConfigureAwait(false);
                }
                catch
                {
                    if (drain.Exception is { InnerExceptions.Count: > 1 } aggregate)
                    {
                        throw aggregate;
                    }

                    throw;
                }
                finally
                {
                    startupRegistration.Dispose();
                }
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

                await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _connectionClosed.Token.ThrowIfCancellationRequested();
                    await stream.WriteAsync(message).ConfigureAwait(false);
                }
                finally
                {
                    _writeLock.Release();
                }
            }

            private async Task StartWriterAsync(IClientStreamWriter<StreamingMessage> requestStream)
            {
                await foreach (StreamingMessage rpcWriteMsg in _outputReader.ReadAllAsync(_connectionClosed.Token).ConfigureAwait(false))
                {
                    await _writeLock.WaitAsync(_connectionClosed.Token).ConfigureAwait(false);
                    try
                    {
                        await requestStream.WriteAsync(rpcWriteMsg).ConfigureAwait(false);
                    }
                    finally
                    {
                        _writeLock.Release();
                    }
                }
            }

            private async Task StartReaderAsync(IAsyncStreamReader<StreamingMessage> responseStream)
            {
                while (await responseStream.MoveNext(_connectionClosed.Token).ConfigureAwait(false))
                {
                    await _processor!.ProcessMessageAsync(responseStream.Current).ConfigureAwait(false);
                }
            }

            private async Task ObserveLoopAndTerminateConnectionAsync(Task loop)
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
                    TerminateConnection();
                }
            }
        }

        [LoggerMessage(1, LogLevel.Error, "The worker gRPC connection failed.")]
        private static partial void LogConnectionFailure(ILogger logger, Exception exception);
    }
}
