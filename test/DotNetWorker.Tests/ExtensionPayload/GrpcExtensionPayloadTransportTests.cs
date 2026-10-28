// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Xunit;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.FunctionRpc;

namespace Microsoft.Azure.Functions.Worker.Tests;

public sealed class GrpcExtensionPayloadTransportTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task PayloadSendAwaitsRealTransportWriteNotQueueAdmission()
    {
        await using ClientContext context = new();
        await context.StartAsync();
        Task payload = context.Client.SendPayloadMessageAsync(CreatePayload(), CancellationToken.None).AsTask();
        WriteRecord write = await context.Transport.First.Task.WaitAsync(Timeout);
        Assert.False(payload.IsCompleted);
        Assert.False(context.Outgoing.Channel.Reader.TryRead(out _));
        write.Completion.SetResult(true);
        await payload.WaitAsync(Timeout);
        Assert.False(context.Transport.ConcurrentWriteDetected);
    }

    [Fact]
    public async Task PayloadAndQueuedLogWritesAreSerializedWithoutDroppingLogs()
    {
        await using ClientContext context = new();
        await context.StartAsync();
        StreamingMessage log = new() { RequestId = "ordinary", RpcLog = new() { Message = "ordinary queued log" } };
        Assert.True(context.Outgoing.Channel.Writer.TryWrite(log));
        WriteRecord first = await context.Transport.First.Task.WaitAsync(Timeout);
        Task payload = context.Client.SendPayloadMessageAsync(CreatePayload(), CancellationToken.None).AsTask();
        Assert.Same(log, first.Message);
        Assert.False(context.Transport.Second.Task.IsCompleted);
        first.Completion.SetResult(true);
        WriteRecord second = await context.Transport.Second.Task.WaitAsync(Timeout);
        Assert.False(payload.IsCompleted);
        second.Completion.SetResult(true);
        await payload.WaitAsync(Timeout);
        Assert.Equal("payload", second.Message.RequestId);
        Assert.False(context.Transport.ConcurrentWriteDetected);
    }

    [Fact]
    public async Task ConnectionShutdownCancelsPayloadWaitingBehindAnInFlightLog()
    {
        await using ClientContext context = new();
        await context.StartAsync();
        Assert.True(context.Outgoing.Channel.Writer.TryWrite(new() { RpcLog = new() { Message = "held log" } }));
        WriteRecord first = await context.Transport.First.Task.WaitAsync(Timeout);
        Task payload = context.Client.SendPayloadMessageAsync(CreatePayload(), context.Client.ConnectionClosed).AsTask();
        Task stopping = context.Client.StopAsync(CancellationToken.None);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => payload).WaitAsync(Timeout);
        Assert.True(context.Client.ConnectionClosed.IsCancellationRequested);
        Assert.False(stopping.IsCompleted);
        Assert.False(first.Completion.Task.IsCompleted);
        Assert.False(context.Transport.Second.Task.IsCompleted);
        first.Completion.SetResult(true);
        await stopping.WaitAsync(Timeout);
        Assert.False(context.Transport.Second.Task.IsCompleted);
        Assert.False(context.Transport.ConcurrentWriteDetected);
    }

    [Fact]
    public async Task PayloadWriteFailurePropagatesInsteadOfReportingQueueSuccess()
    {
        await using ClientContext context = new();
        await context.StartAsync();
        Task payload = context.Client.SendPayloadMessageAsync(CreatePayload(), CancellationToken.None).AsTask();
        WriteRecord write = await context.Transport.First.Task.WaitAsync(Timeout);
        write.Completion.SetException(new IOException("Write failed"));
        await Assert.ThrowsAsync<IOException>(() => payload);
    }

    private static StreamingMessage CreatePayload() => new()
    {
        RequestId = "payload",
        ExtensionPayloadResponse = new() { Chunk = ByteString.CopyFrom(new byte[] { 1, 2, 3 }) }
    };

    private sealed class ClientContext : IAsyncDisposable
    {
        public GrpcHostChannel Outgoing { get; } = new(Channel.CreateUnbounded<StreamingMessage>());
        public ControlledWriter Transport { get; } = new();
        public ControlledReader Incoming { get; } = new();
        public TestLogger<GrpcWorkerClientFactory> Logger { get; } = TestLogger<GrpcWorkerClientFactory>.Create();
        public IWorkerClient Client { get; }

        public ClientContext()
        {
            GrpcWorkerStartupOptions options = new()
            {
                HostEndpoint = new Uri("http://localhost:5000"),
                WorkerId = "test-worker",
                GrpcMaxMessageLength = 4 * 1024 * 1024
            };
            Client = new GrpcWorkerClientFactory.GrpcWorkerClient(Outgoing, options, new NoOpProcessor(), Logger, new TestRpcClient(Transport, Incoming));
        }

        public async Task StartAsync()
        {
            await Client.StartAsync(CancellationToken.None);
            Assert.Equal("test-worker", Transport.StartStream.StartStream.WorkerId);
        }

        public async ValueTask DisposeAsync()
        {
            Transport.CompletePending();
            await Client.StopAsync(CancellationToken.None).WaitAsync(Timeout);
        }
    }

    private sealed class TestRpcClient(ControlledWriter writer, ControlledReader reader) : FunctionRpcClient
    {
        public override AsyncDuplexStreamingCall<StreamingMessage, StreamingMessage> EventStream(
            Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
            => new(writer, reader, Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => reader.Complete());
    }

    private sealed class NoOpProcessor : IMessageProcessor
    {
        public Task ProcessMessageAsync(StreamingMessage message) => Task.CompletedTask;
    }

    private sealed class ControlledReader : IAsyncStreamReader<StreamingMessage>
    {
        private readonly Channel<StreamingMessage> _incoming = Channel.CreateUnbounded<StreamingMessage>();
        public StreamingMessage Current { get; private set; }
        public void Complete(Exception exception = null) => _incoming.Writer.TryComplete(exception);

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            if (await _incoming.Reader.WaitToReadAsync(cancellationToken) && _incoming.Reader.TryRead(out StreamingMessage message))
            {
                Current = message;
                return true;
            }

            return false;
        }
    }

    private sealed class WriteRecord(StreamingMessage message)
    {
        public StreamingMessage Message { get; } = message;
        public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class ControlledWriter : IClientStreamWriter<StreamingMessage>
    {
        private readonly ConcurrentQueue<WriteRecord> _writes = new();
        private int _activeWrites;
        private int _writeCount;
        private int _concurrentWrite;
        private int _complete;
        public StreamingMessage StartStream { get; private set; }
        public TaskCompletionSource<WriteRecord> First { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<WriteRecord> Second { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool ConcurrentWriteDetected => Interlocked.CompareExchange(ref _concurrentWrite, 0, 0) != 0;
        public WriteOptions WriteOptions { get; set; }
        public Task CompleteAsync() => Task.CompletedTask;

        public void CompletePending()
        {
            Interlocked.Exchange(ref _complete, 1);
            foreach (WriteRecord write in _writes)
            {
                write.Completion.TrySetResult(true);
            }
        }

        public async Task WriteAsync(StreamingMessage message)
        {
            if (message.StartStream is not null)
            {
                StartStream = message;
                return;
            }

            if (Interlocked.Increment(ref _activeWrites) != 1)
            {
                Interlocked.Exchange(ref _concurrentWrite, 1);
            }

            WriteRecord write = new(message);
            _writes.Enqueue(write);
            (Interlocked.Increment(ref _writeCount) == 1 ? First : Second).TrySetResult(write);
            if (Interlocked.CompareExchange(ref _complete, 0, 0) != 0)
            {
                write.Completion.TrySetResult(true);
            }

            try
            {
                await write.Completion.Task;
            }
            finally
            {
                Interlocked.Decrement(ref _activeWrites);
            }
        }
    }
}
