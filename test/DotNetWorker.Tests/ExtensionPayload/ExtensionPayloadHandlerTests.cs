// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests;

public sealed class ExtensionPayloadHandlerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    [Theory]
    [InlineData(1024)]
    [InlineData(65536)]
    [InlineData(4194304)]
    public async Task StreamsNonSeekablePayloadAndDisposesBeforeTerminal(int messageLimit)
    {
        byte[] bytes = new byte[150 * 1024];
        new Random(23).NextBytes(bytes);
        TrackingStream stream = new(new MemoryStream(bytes, writable: false));
        TestProvider provider = new(_ => Task.FromResult<Stream>(stream));
        using RecordingWorkerClient client = new();
        client.OnPayload = (message, _) =>
        {
            bool terminal = message.ExtensionPayloadResponse.ContentCase is ExtensionPayloadResponse.ContentOneofCase.Result;
            Assert.Equal(terminal ? 1 : 0, stream.DisposeCount);
            return Task.CompletedTask;
        };

        await CreateHandler(provider, messageLimit: messageLimit).SendAsync("stream", client, CancellationToken.None).WaitAsync(Timeout);
        StreamingMessage[] messages = client.Messages.ToArray();
        Assert.All(messages, message =>
        {
            Assert.Equal("stream", message.RequestId);
            Assert.True(message.CalculateSize() <= messageLimit);
        });
        ulong offset = 0;
        foreach (StreamingMessage message in messages.Take(messages.Length - 1))
        {
            Assert.Equal(offset, message.ExtensionPayloadResponse.Offset);
            Assert.InRange(message.ExtensionPayloadResponse.Chunk.Length, 1, ExtensionPayloadLimits.MaximumChunkBytes);
            offset += (uint)message.ExtensionPayloadResponse.Chunk.Length;
        }

        Assert.Equal(bytes, messages.Take(messages.Length - 1).SelectMany(message => message.ExtensionPayloadResponse.Chunk.ToByteArray()));
        Assert.Equal((ulong)bytes.Length, messages[^1].ExtensionPayloadResponse.Offset);
        Assert.Equal(StatusResult.Types.Status.Success, messages[^1].ExtensionPayloadResponse.Result.Status);
        Assert.Equal(1, stream.DisposeCount);
        Assert.Equal(1, provider.OpenCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InFlightWritePreventsReadAheadAndRetainsStreamOwnership(bool cancel)
    {
        TrackingStream stream = new(new MemoryStream(new byte[150 * 1024]));
        TestProvider provider = new(_ => Task.FromResult<Stream>(stream));
        ExtensionPayloadHandler handler = CreateHandler(provider);
        using RecordingWorkerClient client = new();
        TaskCompletionSource<bool> writing = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.OnPayload = async (message, _) =>
        {
            if (message.ExtensionPayloadResponse.ContentCase is ExtensionPayloadResponse.ContentOneofCase.Chunk)
            {
                writing.TrySetResult(true);
                await release.Task;
            }
        };
        Task transfer = handler.SendAsync("held", client, CancellationToken.None);
        await writing.Task.WaitAsync(Timeout);
        try
        {
            if (cancel)
            {
                Assert.Same(transfer, handler.CancelAsync());
            }

            Assert.False(transfer.IsCompleted);
            Assert.Equal(1, stream.ReadCount);
            Assert.Equal(0, stream.DisposeCount);
        }
        finally
        {
            release.TrySetResult(true);
        }

        if (cancel)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transfer).WaitAsync(Timeout);
            Assert.Single(client.Messages);
        }
        else
        {
            await transfer.WaitAsync(Timeout);
        }

        Assert.Equal(1, stream.DisposeCount);
    }

    [Fact]
    public async Task DuplicateAndCompetingRequestsDoNotOpenAnotherStream()
    {
        TaskCompletionSource<bool> opening = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TestProvider provider = new(async token =>
        {
            opening.TrySetResult(true);
            await release.Task.WaitAsync(token);
            return new MemoryStream(new byte[] { 1, 2, 3 });
        });
        ExtensionPayloadHandler handler = CreateHandler(provider);
        using RecordingWorkerClient client = new();
        Task transfer = handler.SendAsync("active", client, CancellationToken.None);
        await opening.Task.WaitAsync(Timeout);
        try
        {
            Assert.Same(transfer, handler.SendAsync("active", client, CancellationToken.None));
            await handler.SendAsync("competing", client, CancellationToken.None).WaitAsync(Timeout);
            StreamingMessage rejected = Assert.Single(client.Messages);
            Assert.Equal("competing", rejected.RequestId);
            Assert.Equal(StatusResult.Types.Status.Failure, rejected.ExtensionPayloadResponse.Result.Status);
            Assert.Equal(1, provider.OpenCount);
        }
        finally
        {
            release.TrySetResult(true);
        }

        await transfer.WaitAsync(Timeout);
        await handler.SendAsync("fresh", client, CancellationToken.None).WaitAsync(Timeout);
        Assert.Equal(2, provider.OpenCount);
    }

    [Theory]
    [InlineData(false, "unsupported")]
    [InlineData(true, "")]
    public async Task RejectedRequestsDoNotOpenTheProvider(bool supported, string requestId)
    {
        TestProvider provider = new(_ => throw new InvalidOperationException("The provider must not be opened."));
        using RecordingWorkerClient client = new() { SupportsExtensionPayload = supported };
        await CreateHandler(provider).SendAsync(requestId, client, CancellationToken.None).WaitAsync(Timeout);
        Assert.Equal(0, provider.OpenCount);
        Assert.Equal(StatusResult.Types.Status.Failure, Assert.Single(client.Messages).ExtensionPayloadResponse.Result.Status);
    }

    [Fact]
    public async Task CancelledRequestDoesNotOpenTheProvider()
    {
        TestProvider provider = new(_ => throw new InvalidOperationException("The provider must not be opened."));
        using RecordingWorkerClient client = new();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateHandler(provider).SendAsync("cancelled", client, new CancellationToken(canceled: true))).WaitAsync(Timeout);
        Assert.Equal(0, provider.OpenCount);
        Assert.Empty(client.Messages);
    }

    [Fact]
    public async Task ProviderFailureIsLoggedWithoutExposingDetailsInTheResponse()
    {
        IOException failure = new("Private deployment details.");
        TestProvider provider = new(_ => Task.FromException<Stream>(failure));
        TestLogger<ExtensionPayloadHandler> logger = TestLogger<ExtensionPayloadHandler>.Create();
        using RecordingWorkerClient client = new();
        await CreateHandler(provider, logger).SendAsync("failed", client, CancellationToken.None).WaitAsync(Timeout);
        AssertSafeFailure(Assert.Single(client.Messages));
        Assert.Same(failure, Assert.Single(logger.GetLogMessages()).Exception);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ReadAndDisposalFailuresAreRetainedWithoutReportingSuccess(bool failRead, bool failDisposal)
    {
        IOException readFailure = new("Private input details.");
        IOException disposalFailure = new("Private cleanup details.");
        TrackingStream stream = new(new MemoryStream(new byte[] { 1, 2, 3 }))
        {
            ReadFailure = failRead ? readFailure : null,
            DisposalFailure = failDisposal ? disposalFailure : null
        };
        TestProvider provider = new(_ => Task.FromResult<Stream>(stream));
        TestLogger<ExtensionPayloadHandler> logger = TestLogger<ExtensionPayloadHandler>.Create();
        using RecordingWorkerClient client = new();
        await CreateHandler(provider, logger).SendAsync("failed", client, CancellationToken.None).WaitAsync(Timeout);
        AssertSafeFailure(client.Messages.Last());
        Assert.Equal(1, stream.DisposeCount);
        Exception reported = Assert.Single(logger.GetLogMessages()).Exception;
        if (failRead && failDisposal)
        {
            Assert.Equal(new[] { readFailure, disposalFailure }, Assert.IsType<AggregateException>(reported).InnerExceptions);
        }
        else
        {
            Assert.Same(failRead ? readFailure : disposalFailure, reported);
        }
    }

    [Fact]
    public async Task CancellationDoesNotHideAStreamCleanupFailure()
    {
        IOException disposalFailure = new("Stream cleanup failed.");
        TrackingStream stream = new(new MemoryStream(new byte[] { 1, 2, 3 })) { DisposalFailure = disposalFailure };
        ExtensionPayloadHandler handler = CreateHandler(new TestProvider(_ => Task.FromResult<Stream>(stream)));
        using RecordingWorkerClient client = new();
        TaskCompletionSource<bool> writing = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.OnPayload = async (_, _) =>
        {
            writing.TrySetResult(true);
            await release.Task;
        };
        Task transfer = handler.SendAsync("cancelled", client, CancellationToken.None);
        await writing.Task.WaitAsync(Timeout);
        Task draining = handler.CancelAsync();
        release.SetResult(true);
        AggregateException failure = await Assert.ThrowsAsync<AggregateException>(() => draining).WaitAsync(Timeout);
        Assert.Contains(disposalFailure, failure.InnerExceptions);
        Assert.Same(transfer, draining);
        Assert.Equal(1, stream.DisposeCount);
        Assert.Single(client.Messages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TerminalWriteFailureRetainsAnEarlierCleanupFailure(bool cancel)
    {
        IOException disposalFailure = new("Stream cleanup failed.");
        IOException writeFailure = new("Terminal write failed.");
        TrackingStream stream = new(new MemoryStream(new byte[] { 1, 2, 3 })) { DisposalFailure = disposalFailure };
        ExtensionPayloadHandler handler = CreateHandler(new TestProvider(_ => Task.FromResult<Stream>(stream)));
        using RecordingWorkerClient client = new();
        TaskCompletionSource<bool> writingTerminal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.OnPayload = async (message, token) =>
        {
            if (message.ExtensionPayloadResponse.ContentCase is ExtensionPayloadResponse.ContentOneofCase.Result)
            {
                writingTerminal.TrySetResult(true);
                await release.Task;
                token.ThrowIfCancellationRequested();
                throw writeFailure;
            }
        };

        Task transfer = handler.SendAsync("cleanup-then-write-failure", client, CancellationToken.None);
        Task completion = transfer;
        try
        {
            await writingTerminal.Task.WaitAsync(Timeout);
            Assert.Equal(1, stream.DisposeCount);
            if (cancel)
            {
                await client.StopAsync(CancellationToken.None);
                completion = handler.CancelAsync();
                Assert.Same(transfer, completion);
            }
        }
        finally
        {
            release.TrySetResult(true);
        }

        AggregateException failure = await Assert.ThrowsAsync<AggregateException>(() => completion).WaitAsync(Timeout);
        Assert.Contains(disposalFailure, failure.Flatten().InnerExceptions);
        if (cancel)
        {
            Assert.Contains(failure.Flatten().InnerExceptions, exception => exception is OperationCanceledException);
        }
        else
        {
            Assert.Contains(writeFailure, failure.Flatten().InnerExceptions);
        }

        AssertSafeFailure(client.Messages.Last());
        Assert.Equal(1, stream.DisposeCount);
    }

    [Fact]
    public async Task TerminalConstructionFailureRetainsTheOriginalFailure()
    {
        TestProvider provider = new(_ => throw new InvalidOperationException("The provider must not be opened."));
        using RecordingWorkerClient client = new();
        AggregateException failure = await Assert.ThrowsAsync<AggregateException>(() =>
            CreateHandler(provider, messageLimit: 1).SendAsync("too-small", client, CancellationToken.None)).WaitAsync(Timeout);
        Assert.Collection(failure.InnerExceptions,
            exception => Assert.Contains("cannot fit an extension payload chunk", exception.Message),
            exception => Assert.Contains("cannot fit a correlated extension payload response", exception.Message));
        Assert.Equal(0, provider.OpenCount);
        Assert.Empty(client.Messages);
    }

    [Fact]
    public async Task TerminalWriteFailureWithoutAnEarlierFailureIsNotWrapped()
    {
        IOException writeFailure = new("Terminal write failed.");
        TrackingStream stream = new(new MemoryStream(new byte[] { 1, 2, 3 }));
        using RecordingWorkerClient client = new();
        client.OnPayload = (message, _) => message.ExtensionPayloadResponse.ContentCase is ExtensionPayloadResponse.ContentOneofCase.Result
            ? Task.FromException(writeFailure)
            : Task.CompletedTask;
        Exception failure = await Assert.ThrowsAsync<IOException>(() =>
            CreateHandler(new TestProvider(_ => Task.FromResult<Stream>(stream))).SendAsync("terminal", client, CancellationToken.None));
        Assert.Same(writeFailure, failure);
        Assert.Equal(1, stream.DisposeCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task StreamingEnforcesTheArchiveLimitWithoutRequiringLength(int excess)
    {
        TrackingStream stream = new(new GeneratedStream(ExtensionPayloadLimits.MaximumArchiveBytes + excess));
        TestProvider provider = new(_ => Task.FromResult<Stream>(stream));
        Mock<IWorkerClient> client = new(MockBehavior.Strict);
        client.SetupGet(value => value.SupportsExtensionPayload).Returns(true);
        client.SetupGet(value => value.ConnectionClosed).Returns(CancellationToken.None);
        long sent = 0;
        StreamingMessage terminal = null;
        client.Setup(value => value.SendPayloadMessageAsync(It.IsAny<StreamingMessage>(), It.IsAny<CancellationToken>()))
            .Returns((StreamingMessage message, CancellationToken _) =>
            {
                if (message.ExtensionPayloadResponse.ContentCase is ExtensionPayloadResponse.ContentOneofCase.Chunk)
                {
                    Assert.Equal((ulong)sent, message.ExtensionPayloadResponse.Offset);
                    sent += message.ExtensionPayloadResponse.Chunk.Length;
                    Assert.True(sent <= ExtensionPayloadLimits.MaximumArchiveBytes);
                }
                else
                {
                    Assert.Null(terminal);
                    terminal = message;
                }

                return ValueTask.CompletedTask;
            });

        await CreateHandler(provider).SendAsync("limit", client.Object, CancellationToken.None).WaitAsync(Timeout);
        Assert.Equal(ExtensionPayloadLimits.MaximumArchiveBytes, sent);
        Assert.NotNull(terminal);
        Assert.Equal((ulong)sent, terminal.ExtensionPayloadResponse.Offset);
        Assert.Equal(excess == 0 ? StatusResult.Types.Status.Success : StatusResult.Types.Status.Failure,
            terminal.ExtensionPayloadResponse.Result.Status);
        Assert.Equal(1, stream.DisposeCount);
    }

    [Fact]
    public async Task CancellationCallbackFailureStillDrainsTheTransfer()
    {
        IOException cancellationFailure = new("Cancellation callback failed.");
        CancellationTokenRegistration registration = default;
        TrackingStream stream = new(new MemoryStream(new byte[] { 1, 2, 3 }));
        TestProvider provider = new(token =>
        {
            registration = token.Register(() => throw cancellationFailure);
            return Task.FromResult<Stream>(stream);
        });
        ExtensionPayloadHandler handler = CreateHandler(provider);
        using RecordingWorkerClient client = new();
        TaskCompletionSource<bool> writing = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.OnPayload = async (_, _) =>
        {
            writing.TrySetResult(true);
            await release.Task;
        };
        Task transfer = handler.SendAsync("cancelled", client, CancellationToken.None);
        try
        {
            await writing.Task.WaitAsync(Timeout);
            Task draining;
            try
            {
                draining = handler.CancelAsync();
                Assert.False(draining.IsCompleted);
                Assert.Equal(0, stream.DisposeCount);
            }
            finally
            {
                release.TrySetResult(true);
            }

            AggregateException failure = await Assert.ThrowsAsync<AggregateException>(() => draining).WaitAsync(Timeout);
            Assert.Contains(cancellationFailure, failure.Flatten().InnerExceptions);
            Assert.Equal(1, stream.DisposeCount);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transfer);
        }
        finally
        {
            release.TrySetResult(true);
            registration.Dispose();
        }
    }

    private static ExtensionPayloadHandler CreateHandler(IExtensionPayloadProvider provider, ILogger<ExtensionPayloadHandler> logger = null,
        int messageLimit = 4 * 1024 * 1024)
        => new(provider, Options.Create(new GrpcWorkerStartupOptions { GrpcMaxMessageLength = messageLimit }),
            logger ?? NullLogger<ExtensionPayloadHandler>.Instance);

    private static void AssertSafeFailure(StreamingMessage message)
    {
        Assert.Equal(ExtensionPayloadResponse.ContentOneofCase.Result, message.ExtensionPayloadResponse.ContentCase);
        Assert.Equal(StatusResult.Types.Status.Failure, message.ExtensionPayloadResponse.Result.Status);
        Assert.Equal("Extension payload transfer failed. See the worker logs for details.", message.ExtensionPayloadResponse.Result.Result);
        Assert.Null(message.ExtensionPayloadResponse.Result.Exception);
    }

    private sealed class TestProvider(Func<CancellationToken, Task<Stream>> open) : IExtensionPayloadProvider
    {
        private int _openCount;
        public int OpenCount => Volatile.Read(ref _openCount);

        public Task<Stream> OpenReadAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _openCount);
            return open(cancellationToken);
        }
    }

    private sealed class TrackingStream(Stream inner) : Stream
    {
        public int ReadCount { get; private set; }
        public int DisposeCount { get; private set; }
        public Exception ReadFailure { get; init; }
        public Exception DisposalFailure { get; init; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Assert.Equal(0, DisposeCount);
            cancellationToken.ThrowIfCancellationRequested();
            ReadCount++;
            return ReadFailure is null ? inner.ReadAsync(buffer, offset, count, cancellationToken) : Task.FromException<int>(ReadFailure);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeCount++;
                inner.Dispose();
                if (DisposalFailure is not null)
                {
                    throw DisposalFailure;
                }
            }

            base.Dispose(disposing);
        }
    }

    private sealed class GeneratedStream(long length) : Stream
    {
        private long _remaining = length;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = (int)Math.Min(count, _remaining);
            Array.Clear(buffer, offset, read);
            _remaining -= read;
            return read;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Read(buffer, offset, count));
        }
    }
}
