// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Azure.Functions.Tests;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Handlers;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using RpcException = Grpc.Core.RpcException;

namespace Microsoft.Azure.Functions.Worker.Tests;

public sealed partial class GrpcExtensionPayloadTransportTests
{
    [Theory]
    [InlineData("worker-stop")]
    [InlineData("expired-stop-token")]
    [InlineData("connection-close")]
    [InlineData("cancellation-callback-failure")]
    public Task WorkerShutdownRetainsArchiveUntilControlledWriteFinishes(string cause)
        => VerifyWorkerShutdownAsync(cause, abortWritesOnDispose: false);

    [Theory]
    [InlineData("worker-stop")]
    [InlineData("expired-stop-token")]
    [InlineData("connection-close")]
    [InlineData("cancellation-callback-failure")]
    public Task WorkerShutdownAbortsPendingWriteAndCleansArchive(string cause)
        => VerifyWorkerShutdownAsync(cause, abortWritesOnDispose: true);

    private async Task VerifyWorkerShutdownAsync(string cause, bool abortWritesOnDispose)
    {
        string root = Path.Combine(Path.GetTempPath(), "ExtensionPayloadShutdown-" + Guid.NewGuid().ToString("N"));
        string temporary = Path.Combine(root, "temporary");
        Directory.CreateDirectory(temporary);
        Directory.CreateDirectory(Path.Combine(root, ".azurefunctions"));
        File.WriteAllText(Path.Combine(root, "extensions.json"), "{\"extensions\":[]}");
        File.WriteAllText(Path.Combine(root, ".azurefunctions", "extension.dll"), "extension contents");
        using TestScopedAppContextBaseDirectory application = new(root + Path.DirectorySeparatorChar);
        using TestScopedEnvironmentVariable tmp = new("TMP", temporary);
        using TestScopedEnvironmentVariable temp = new("TEMP", temporary);
        using TestScopedEnvironmentVariable tmpdir = new("TMPDIR", temporary);

        ClientContext context = null;
        Mock<IWorkerClientFactory> factory = new();
        factory.Setup(value => value.CreateClient(It.IsAny<IMessageProcessor>())).Returns((IMessageProcessor processor) =>
        {
            context = new ClientContext(processor, abortWritesOnDispose: abortWritesOnDispose);
            return context.Client;
        });
        ObservedProvider provider = new(new ExtensionPayloadProvider());
        IOException cancellationFailure = new("Payload cancellation callback failed.");
        CancellationTokenRegistration registration = default;
        if (cause == "cancellation-callback-failure")
        {
            provider.OnOpen = token => registration = token.Register(() => throw cancellationFailure);
        }

        ExtensionPayloadHandler handler = new(provider, Options.Create(new GrpcWorkerStartupOptions { GrpcMaxMessageLength = 4 * 1024 * 1024 }),
            NullLogger<ExtensionPayloadHandler>.Instance);
        GrpcWorker worker = new(Mock.Of<IFunctionsApplication>(), factory.Object, Mock.Of<IMethodInfoLocator>(), Options.Create(new WorkerOptions()),
            Mock.Of<IFunctionMetadataManager>(), new ApplicationLifetime(NullLogger<ApplicationLifetime>.Instance), Mock.Of<IInvocationHandler>(),
            handler, NullLogger<GrpcWorker>.Instance);
        try
        {
            await worker.StartAsync(CancellationToken.None);
            context.Incoming.Add(new() { RequestId = "shutdown", GetExtensionPayloadRequest = new() });
            WriteRecord write = await context.Transport.First.Task.WaitAsync(Timeout);
            Assert.Equal(ExtensionPayloadResponse.ContentOneofCase.Chunk, write.Message.ExtensionPayloadResponse.ContentCase);
            Stream payload = await provider.Opened.Task.WaitAsync(Timeout);
            string archive = Assert.Single(Directory.EnumerateFiles(temporary, "*.zip", SearchOption.AllDirectories));
            if (cause == "connection-close")
            {
                context.Incoming.Complete();
                await context.RpcClient.Disposed.Task.WaitAsync(Timeout);
            }

            Task stopping = worker.StopAsync(new CancellationToken(canceled: cause == "expired-stop-token"));
            await context.RpcClient.Disposed.Task.WaitAsync(Timeout);
            Assert.True(context.Client.ConnectionClosed.IsCancellationRequested);
            Assert.False(context.Outgoing.Channel.Writer.TryWrite(new()));

            if (!abortWritesOnDispose)
            {
                Assert.False(stopping.IsCompleted);
                Assert.True(payload.CanRead);
                Assert.True(File.Exists(archive));
                write.Completion.SetException(new RpcException(new Status(StatusCode.Cancelled, "Transport write aborted.")));
            }

            if (cause == "cancellation-callback-failure")
            {
                AggregateException failure = await Assert.ThrowsAsync<AggregateException>(() => stopping).WaitAsync(Timeout);
                Assert.Contains(cancellationFailure, failure.Flatten().InnerExceptions);
            }
            else
            {
                await stopping.WaitAsync(Timeout);
            }

            Assert.False(payload.CanRead);
            Assert.True(write.Completion.Task.IsFaulted);
            Assert.Empty(Directory.EnumerateFileSystemEntries(temporary));
            Assert.False(context.Transport.Second.Task.IsCompleted);
            Assert.Equal(1, context.RpcClient.DisposeCount);
            Assert.False(context.Transport.ConcurrentWriteDetected);
        }
        finally
        {
            try
            {
                context?.Transport.CompletePending();
                await worker.StopAsync(CancellationToken.None).WaitAsync(Timeout);
            }
            finally
            {
                registration.Dispose();
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WorkerShutdownRetainsCleanupFailureWhenTerminalWriteIsAborted()
    {
        IOException cleanupFailure = new("Payload cleanup failed.");
        Stream payload = new CleanupFailureStream(cleanupFailure);
        Mock<IExtensionPayloadProvider> provider = new();
        provider.Setup(value => value.OpenReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(payload);
        ClientContext context = null;
        Mock<IWorkerClientFactory> factory = new();
        factory.Setup(value => value.CreateClient(It.IsAny<IMessageProcessor>())).Returns((IMessageProcessor processor) =>
        {
            context = new ClientContext(processor, abortWritesOnDispose: true);
            return context.Client;
        });
        ExtensionPayloadHandler handler = new(provider.Object,
            Options.Create(new GrpcWorkerStartupOptions { GrpcMaxMessageLength = 4 * 1024 * 1024 }),
            NullLogger<ExtensionPayloadHandler>.Instance);
        GrpcWorker worker = new(Mock.Of<IFunctionsApplication>(), factory.Object, Mock.Of<IMethodInfoLocator>(), Options.Create(new WorkerOptions()),
            Mock.Of<IFunctionMetadataManager>(), new ApplicationLifetime(NullLogger<ApplicationLifetime>.Instance), Mock.Of<IInvocationHandler>(),
            handler, NullLogger<GrpcWorker>.Instance);
        try
        {
            await worker.StartAsync(CancellationToken.None);
            context.Incoming.Add(new() { RequestId = "cleanup-then-shutdown", GetExtensionPayloadRequest = new() });
            WriteRecord chunk = await context.Transport.First.Task.WaitAsync(Timeout);
            chunk.Completion.SetResult(true);
            WriteRecord terminal = await context.Transport.Second.Task.WaitAsync(Timeout);
            Assert.Equal(ExtensionPayloadResponse.ContentOneofCase.Result, terminal.Message.ExtensionPayloadResponse.ContentCase);
            Assert.Equal(StatusResult.Types.Status.Failure, terminal.Message.ExtensionPayloadResponse.Result.Status);
            Assert.False(payload.CanRead);

            AggregateException failure = await Assert.ThrowsAsync<AggregateException>(() =>
                worker.StopAsync(CancellationToken.None)).WaitAsync(Timeout);
            Assert.Contains(cleanupFailure, failure.Flatten().InnerExceptions);
            Assert.Contains(failure.Flatten().InnerExceptions,
                exception => exception is RpcException rpc && rpc.StatusCode == StatusCode.Cancelled);
            Assert.True(terminal.Completion.Task.IsFaulted);
            Assert.Equal(1, context.RpcClient.DisposeCount);
        }
        finally
        {
            context?.Transport.CompletePending();
            await worker.StopAsync(CancellationToken.None).WaitAsync(Timeout);
        }
    }

    private sealed class CleanupFailureStream(Exception failure) : MemoryStream(new byte[] { 1, 2, 3 })
    {
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                throw failure;
            }
        }
    }

    private sealed class ObservedProvider(IExtensionPayloadProvider inner) : IExtensionPayloadProvider
    {
        public TaskCompletionSource<Stream> Opened { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Action<CancellationToken> OnOpen { get; set; }

        public async Task<Stream> OpenReadAsync(CancellationToken cancellationToken)
        {
            OnOpen?.Invoke(cancellationToken);
            Stream stream = await inner.OpenReadAsync(cancellationToken);
            Opened.TrySetResult(stream);
            return stream;
        }
    }
}
