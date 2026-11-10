// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Tests;

internal sealed class RecordingWorkerClient : IWorkerClient, IDisposable
{
    private readonly CancellationTokenSource _closed = new();
    public ConcurrentQueue<StreamingMessage> Messages { get; } = new();
    public Func<StreamingMessage, CancellationToken, Task> OnPayload { get; set; }
    public Action<StreamingMessage> OnMessage { get; set; }
    public bool SupportsExtensionPayload { get; init; } = true;
    public CancellationToken ConnectionClosed => _closed.Token;
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _closed.Cancel();
        return Task.CompletedTask;
    }

    public ValueTask SendMessageAsync(StreamingMessage message)
    {
        Messages.Enqueue(message);
        OnMessage?.Invoke(message);
        return ValueTask.CompletedTask;
    }

    public async ValueTask SendPayloadMessageAsync(StreamingMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Messages.Enqueue(message);
        if (OnPayload is not null)
        {
            await OnPayload(message, cancellationToken);
        }
    }

    public void Dispose() => _closed.Dispose();
}
