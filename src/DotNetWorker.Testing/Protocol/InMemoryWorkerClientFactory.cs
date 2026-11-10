// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Testing.Protocol;

internal sealed class InMemoryWorkerClientFactory : IWorkerClientFactory, IAsyncDisposable
{
    private readonly InMemoryFunctionsHost _host;
    private readonly GrpcHostChannel _outputChannel;
    private readonly ConcurrentBag<InMemoryWorkerClient> _clients = new();
    private int _disposed;

    public InMemoryWorkerClientFactory(InMemoryFunctionsHost host, GrpcHostChannel outputChannel)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _outputChannel = outputChannel ?? throw new ArgumentNullException(nameof(outputChannel));
    }

    public IWorkerClient CreateClient(IMessageProcessor messageProcessor)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

        var client = new InMemoryWorkerClient(_host, _outputChannel, messageProcessor);
        _clients.Add(client);
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        foreach (InMemoryWorkerClient client in _clients)
        {
            await client.DisposeAsync();
        }
    }

    private sealed class InMemoryWorkerClient : IWorkerClient, IAsyncDisposable
    {
        private readonly InMemoryFunctionsHost _host;
        private readonly GrpcHostChannel _outputChannel;
        private readonly IMessageProcessor _messageProcessor;
        private readonly CancellationTokenSource _shutdown = new();
        private CancellationTokenRegistration _startCancellationRegistration;
        private Task? _logPump;
        private int _started;

        internal InMemoryWorkerClient(
            InMemoryFunctionsHost host,
            GrpcHostChannel outputChannel,
            IMessageProcessor messageProcessor)
        {
            _host = host;
            _outputChannel = outputChannel;
            _messageProcessor = messageProcessor ?? throw new ArgumentNullException(nameof(messageProcessor));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref _started, 1) != 0)
            {
                throw new InvalidOperationException("The in-memory worker client has already been started.");
            }

            _startCancellationRegistration = cancellationToken.Register(
                static state => ((CancellationTokenSource)state!).Cancel(),
                _shutdown);
            _host.Connect(_messageProcessor);
            await _host.AcceptWorkerMessageAsync(new StreamingMessage
            {
                StartStream = new StartStream { WorkerId = InMemoryFunctionsHost.TestWorkerId }
            });
            _logPump = PumpLogsAsync(_shutdown.Token);
        }

        public ValueTask SendMessageAsync(StreamingMessage message)
            => _host.AcceptWorkerMessageAsync(message);

        public async ValueTask DisposeAsync()
        {
            _shutdown.Cancel();

            if (_logPump is not null)
            {
                try
                {
                    await _logPump;
                }
                catch (OperationCanceledException)
                {
                }
            }

            _startCancellationRegistration.Dispose();
            _shutdown.Dispose();
        }

        private async Task PumpLogsAsync(CancellationToken cancellationToken)
        {
            await foreach (StreamingMessage message in _outputChannel.Channel.Reader.ReadAllAsync(cancellationToken))
            {
                await _host.AcceptWorkerMessageAsync(message);
            }
        }
    }
}
