// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests;

public sealed partial class GrpcExtensionPayloadTransportTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StartupFailureTerminatesConnectionAndClosesOutput(bool beforeCallCreation)
    {
        await using ClientContext context = new();
        InvalidOperationException failure = new("Startup failed.");
        if (beforeCallCreation)
        {
            context.RpcClient.OnCreate = () => throw failure;
        }
        else
        {
            context.Transport.StartWriteCompletion = Task.FromException(failure);
        }

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => context.Client.StartAsync(CancellationToken.None)));
        Assert.True(context.Client.ConnectionClosed.IsCancellationRequested);
        Assert.False(context.Outgoing.Channel.Writer.TryWrite(new()));
        await context.Client.StopAsync(CancellationToken.None).WaitAsync(Timeout);
        Assert.Equal(beforeCallCreation ? 0 : 1, context.RpcClient.DisposeCount);
    }

    [Theory]
    [InlineData("reader-completion")]
    [InlineData("reader-failure")]
    [InlineData("writer-completion")]
    [InlineData("writer-failure")]
    public async Task EitherLoopTerminatesTheConnectionOnce(string cause)
    {
        await using ClientContext context = new();
        await context.StartAsync();
        switch (cause)
        {
            case "reader-completion":
                context.Incoming.Complete();
                break;
            case "reader-failure":
                context.Incoming.Complete(new IOException("Reader failed."));
                break;
            case "writer-completion":
                context.Outgoing.Channel.Writer.TryComplete();
                break;
            case "writer-failure":
                Assert.True(context.Outgoing.Channel.Writer.TryWrite(new()));
                WriteRecord write = await context.Transport.First.Task.WaitAsync(Timeout);
                write.Completion.SetException(new IOException("Writer failed."));
                break;
        }

        await context.RpcClient.Disposed.Task.WaitAsync(Timeout);
        await context.Client.StopAsync(CancellationToken.None).WaitAsync(Timeout);
        Assert.True(context.Client.ConnectionClosed.IsCancellationRequested);
        Assert.False(context.Outgoing.Channel.Writer.TryWrite(new()));
        Assert.Equal(1, context.RpcClient.DisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentStopCallsDrainTheSameInFlightWriter(bool allowSynchronousContinuations)
    {
        await using ClientContext context = new(allowSynchronousContinuations: allowSynchronousContinuations);
        await context.StartAsync();
        Assert.True(context.Outgoing.Channel.Writer.TryWrite(new()));
        WriteRecord write = await context.Transport.First.Task.WaitAsync(Timeout);
        Task first = context.Client.StopAsync(CancellationToken.None);
        Task second = context.Client.StopAsync(new CancellationToken(canceled: true));
        Assert.False(first.IsCompleted);
        Assert.False(second.IsCompleted);
        Assert.False(context.Outgoing.Channel.Writer.TryWrite(new()));
        write.Completion.SetResult(true);
        await Task.WhenAll(first, second).WaitAsync(Timeout);
        Assert.Equal(1, context.RpcClient.DisposeCount);
    }

    [Fact]
    public async Task StartupCancellationAbortsTheInFlightStartWrite()
    {
        await using ClientContext context = new();
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        context.Transport.StartWriteCompletion = release.Task;
        context.RpcClient.OnDispose = () => release.TrySetCanceled(cancellation.Token);
        Task starting = context.Client.StartAsync(cancellation.Token);
        await context.Transport.StartWriteStarted.Task.WaitAsync(Timeout);
        try
        {
            cancellation.Cancel();
            await context.RpcClient.Disposed.Task.WaitAsync(Timeout);
        }
        finally
        {
            release.TrySetCanceled(cancellation.Token);
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => starting).WaitAsync(Timeout);
        await context.Client.StopAsync(CancellationToken.None).WaitAsync(Timeout);
        Assert.Equal(1, context.RpcClient.DisposeCount);
        Assert.False(context.Outgoing.Channel.Writer.TryWrite(new()));
    }

    [Fact]
    public async Task TerminationDuringCallCreationDisposesTheUnpublishedCall()
    {
        await using ClientContext context = new();
        using ManualResetEventSlim release = new();
        TaskCompletionSource<bool> creating = new(TaskCreationOptions.RunContinuationsAsynchronously);
        context.RpcClient.OnCreate = () =>
        {
            creating.TrySetResult(true);
            Assert.True(release.Wait(Timeout));
        };
        Task starting = Task.Run(() => context.Client.StartAsync(CancellationToken.None));
        await creating.Task.WaitAsync(Timeout);
        Task stopping;
        try
        {
            stopping = context.Client.StopAsync(CancellationToken.None);
            Assert.False(stopping.IsCompleted);
        }
        finally
        {
            release.Set();
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => starting).WaitAsync(Timeout);
        await stopping.WaitAsync(Timeout);
        Assert.Equal(1, context.RpcClient.DisposeCount);
        Assert.False(context.Outgoing.Channel.Writer.TryWrite(new()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnpublishedCallCleanupFailureIsRetainedByRepeatedShutdown(bool failCancellation)
    {
        ClientContext context = new();
        using ManualResetEventSlim release = new();
        TaskCompletionSource<bool> creating = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IOException disposalFailure = new("Unpublished call disposal failed.");
        InvalidOperationException cancellationFailure = new("Connection cancellation failed.");
        using CancellationTokenRegistration registration = failCancellation
            ? context.Client.ConnectionClosed.Register(() => throw cancellationFailure)
            : default;
        context.RpcClient.OnCreate = () =>
        {
            creating.TrySetResult(true);
            Assert.True(release.Wait(Timeout));
        };
        context.RpcClient.OnDispose = () => throw disposalFailure;
        Task starting = Task.Run(() => context.Client.StartAsync(CancellationToken.None));
        Task stopping;
        try
        {
            await creating.Task.WaitAsync(Timeout);
            stopping = context.Client.StopAsync(CancellationToken.None);
            Assert.False(stopping.IsCompleted);
        }
        finally
        {
            release.Set();
        }

        Assert.Same(disposalFailure, await Assert.ThrowsAsync<IOException>(() => starting).WaitAsync(Timeout));
        Exception first;
        if (failCancellation)
        {
            AggregateException failure = await Assert.ThrowsAsync<AggregateException>(() => stopping).WaitAsync(Timeout);
            Assert.Contains(disposalFailure, failure.Flatten().InnerExceptions);
            Assert.Contains(cancellationFailure, failure.Flatten().InnerExceptions);
            first = failure;
        }
        else
        {
            first = await Assert.ThrowsAsync<IOException>(() => stopping).WaitAsync(Timeout);
            Assert.Same(disposalFailure, first);
        }

        Exception repeated = await Record.ExceptionAsync(() => context.Client.StopAsync(CancellationToken.None).WaitAsync(Timeout));
        Assert.Same(first, repeated);
        Assert.Equal(1, context.RpcClient.DisposeCount);
        Assert.True(context.Client.ConnectionClosed.IsCancellationRequested);
        Assert.False(context.Outgoing.Channel.Writer.TryWrite(new()));
    }

    [Fact]
    public async Task StopBeforeStartLeavesTheClientTerminal()
    {
        await using ClientContext context = new();
        await context.Client.StopAsync(CancellationToken.None).WaitAsync(Timeout);
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.Client.StartAsync(CancellationToken.None));
        Assert.False(context.Outgoing.Channel.Writer.TryWrite(new()));
        Assert.Equal(0, context.RpcClient.DisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ImmediateReaderCompletionDoesNotRaceStartupPublication(bool allowSynchronousContinuations)
    {
        await using ClientContext context = new(allowSynchronousContinuations: allowSynchronousContinuations);
        context.Incoming.Complete();
        await context.StartAsync();
        await context.Client.StopAsync(CancellationToken.None).WaitAsync(Timeout);
        Assert.Equal(1, context.RpcClient.DisposeCount);
        Assert.False(context.Outgoing.Channel.Writer.TryWrite(new()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReentrantStopFromCancellationCallbackDoesNotWaitOnItself(bool allowSynchronousContinuations)
    {
        await using ClientContext context = new(allowSynchronousContinuations: allowSynchronousContinuations);
        using CancellationTokenSource cancellation = new();
        await context.Client.StartAsync(cancellation.Token);
        Task reentrantStop = null;
        using CancellationTokenRegistration registration = context.Client.ConnectionClosed.Register(
            () => reentrantStop = context.Client.StopAsync(CancellationToken.None));
        cancellation.Cancel();
        Assert.NotNull(reentrantStop);
        await reentrantStop.WaitAsync(Timeout);
        Assert.Equal(1, context.RpcClient.DisposeCount);
    }

    [Fact]
    public async Task TerminationAttemptsDisposalAndRetainsCancellationAndDisposalFailures()
    {
        ClientContext context = new();
        await context.StartAsync();
        InvalidOperationException cancellationFailure = new("Cancellation failed.");
        IOException disposalFailure = new("Disposal failed.");
        using CancellationTokenRegistration registration = context.Client.ConnectionClosed.Register(() => throw cancellationFailure);
        context.RpcClient.OnDispose = () => throw disposalFailure;

        AggregateException failure = await Assert.ThrowsAsync<AggregateException>(
            () => context.Client.StopAsync(CancellationToken.None)).WaitAsync(Timeout);
        Assert.Contains(cancellationFailure, failure.Flatten().InnerExceptions);
        Assert.Contains(disposalFailure, failure.Flatten().InnerExceptions);
        Assert.False(context.Outgoing.Channel.Writer.TryWrite(new()));
        Assert.Equal(1, context.RpcClient.DisposeCount);
        Assert.Contains(context.Logger.GetLogMessages(), message => ReferenceEquals(message.Exception, failure));

        AggregateException repeated = await Assert.ThrowsAsync<AggregateException>(
            () => context.Client.StopAsync(CancellationToken.None)).WaitAsync(Timeout);
        Assert.Same(failure, repeated);
        Assert.Equal(1, context.RpcClient.DisposeCount);
    }
}
