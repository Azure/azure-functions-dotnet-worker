// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Tests;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Grpc.NativeHostIntegration;
using Microsoft.Azure.Functions.Worker.Handlers;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests;

public sealed partial class ExtensionPayloadTests : IDisposable
{
    private const string Manifest = "{\"extensions\":[{\"name\":\"Binding\",\"typeName\":\"Binding.Startup, binding\",\"hintPath\":\"./.azurefunctions/nested/binding.dll\"}]}";
    private readonly string _root;
    private readonly string _temporaryRoot;
    private readonly TestScopedEnvironmentVariable[] _environment;

    public ExtensionPayloadTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ExtensionPayloadTests-" + Guid.NewGuid().ToString("N"));
        _temporaryRoot = Path.Combine(_root, "temporary");
        Directory.CreateDirectory(_temporaryRoot);
        Directory.CreateDirectory(Path.Combine(_root, ".azurefunctions"));
        File.WriteAllText(Path.Combine(_root, "extensions.json"), Manifest);
        _environment =
        [
            new(ExtensionPayload.ApplicationDirectoryVariable, _root),
            new("TMP", _temporaryRoot),
            new("TEMP", _temporaryRoot),
            new("TMPDIR", _temporaryRoot),
        ];
    }

    public void Dispose()
    {
        for (int index = _environment.Length - 1; index >= 0; index--)
        {
            _environment[index].Dispose();
        }

        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task ArchivePreservesManifestAndRecursiveFilesOnly()
    {
        string nested = Path.Combine(_root, ".azurefunctions", "nested");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(nested, "binding.dll"), "binding");
        File.WriteAllText(Path.Combine(nested, ".hidden"), "hidden");
        File.WriteAllText(Path.Combine(_root, "worker.config.json"), "not included");
        File.WriteAllText(Path.Combine(_root, "functions.metadata"), "not included");
        string temporary;
        using (FileStream stream = await ExtensionPayload.CreateArchiveAsync(CancellationToken.None))
        {
            temporary = stream.Name;
            using ZipArchive archive = new(stream, ZipArchiveMode.Read, leaveOpen: true);
            Assert.NotNull(archive.GetEntry(".azurefunctions/nested/binding.dll"));
            Assert.NotNull(archive.GetEntry(".azurefunctions/nested/.hidden"));
            Assert.Null(archive.GetEntry("worker.config.json"));
            Assert.Null(archive.GetEntry("functions.metadata"));
            using StreamReader reader = new(archive.GetEntry("extensions.json")!.Open());
            Assert.Equal(Manifest, await reader.ReadToEndAsync());
        }

        Assert.False(File.Exists(temporary));
        AssertNoTemporaryArchives();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CapabilityRequiresOnlyEligibleTransport(bool supported)
    {
        using TestScopedEnvironmentVariable directory = new(ExtensionPayload.ApplicationDirectoryVariable, "relative");
        WorkerOptions options = new();
        options.Capabilities[WorkerCapabilities.ExtensionPayload] = "application override";
        WorkerInitResponse init = GrpcWorker.WorkerInitRequestHandler(new(), options, supported);
        FunctionEnvironmentReloadResponse reload = GrpcWorker.EnvironmentReloadRequestHandler(options, supported);
        Assert.Equal(StatusResult.Types.Status.Success, init.Result.Status);
        Assert.Equal(StatusResult.Types.Status.Success, reload.Result.Status);
        Assert.Equal(supported, init.Capabilities.ContainsKey(WorkerCapabilities.ExtensionPayload));
        Assert.Equal(supported, reload.Capabilities.ContainsKey(WorkerCapabilities.ExtensionPayload));
        if (supported)
        {
            Assert.Equal("1", init.Capabilities[WorkerCapabilities.ExtensionPayload]);
            Assert.Equal("1", reload.Capabilities[WorkerCapabilities.ExtensionPayload]);
        }

        AssertNoTemporaryArchives();
    }

    [Theory]
    [InlineData("environment")]
    [InlineData("manifest")]
    [InlineData("payload")]
    public async Task MissingInputsAreValidatedOnlyWhenPayloadIsRequested(string missing)
    {
        using TestScopedEnvironmentVariable directory = new(ExtensionPayload.ApplicationDirectoryVariable,
            string.Equals(missing, "environment", StringComparison.Ordinal) ? null : _root);
        if (string.Equals(missing, "manifest", StringComparison.Ordinal))
        {
            File.Delete(Path.Combine(_root, "extensions.json"));
        }
        else if (string.Equals(missing, "payload", StringComparison.Ordinal))
        {
            Directory.Delete(Path.Combine(_root, ".azurefunctions"));
        }

        AssertPayloadSupportIsAdvertised();
        await AssertPayloadRequestFailsAsync();
    }

    [Theory]
    [InlineData("relative")]
    [InlineData("C:relative")]
    [InlineData("\\rooted")]
    [InlineData(" ")]
    public async Task MalformedRootIsValidatedOnlyWhenPayloadIsRequested(string root)
    {
        using TestScopedEnvironmentVariable directory = new(ExtensionPayload.ApplicationDirectoryVariable, root);
        AssertPayloadSupportIsAdvertised();
        await AssertPayloadRequestFailsAsync();
    }

    [Fact]
    public async Task NonDirectoryRootIsValidatedOnlyWhenPayloadIsRequested()
    {
        string path = Path.Combine(_root, "not-a-directory");
        File.WriteAllText(path, "file");
        using TestScopedEnvironmentVariable directory = new(ExtensionPayload.ApplicationDirectoryVariable, path);
        AssertPayloadSupportIsAdvertised();
        await AssertPayloadRequestFailsAsync();
    }

    [Fact]
    public async Task UnreadableManifestIsValidatedOnlyWhenPayloadIsRequested()
    {
        using FileStream held = File.Open(Path.Combine(_root, "extensions.json"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        AssertPayloadSupportIsAdvertised();
        await AssertPayloadRequestFailsAsync();
    }

    [Theory]
    [InlineData(1024)]
    [InlineData(65536)]
    [InlineData(4194304)]
    public async Task StreamsCorrelatedBoundedChunksAndExactlyOneTerminal(int messageLimit)
    {
        WriteData(200 * 1024);
        using RecordingClient client = new();
        await CreateProducer(messageLimit).SendAsync("payload-operation", client, CancellationToken.None);
        StreamingMessage[] messages = client.Messages.ToArray();
        Assert.True(messages.Length > 2);
        ulong offset = 0;
        using MemoryStream bytes = new();
        foreach (StreamingMessage message in messages.Take(messages.Length - 1))
        {
            Assert.Equal("payload-operation", message.RequestId);
            Assert.Equal(ExtensionPayloadResponse.ContentOneofCase.Chunk, message.ExtensionPayloadResponse.ContentCase);
            Assert.InRange(message.ExtensionPayloadResponse.Chunk.Length, 1, ExtensionPayload.MaximumChunkBytes);
            Assert.True(message.CalculateSize() <= messageLimit);
            Assert.Equal(offset, message.ExtensionPayloadResponse.Offset);
            byte[] chunk = message.ExtensionPayloadResponse.Chunk.ToByteArray();
            await bytes.WriteAsync(chunk);
            offset += (uint)chunk.Length;
        }

        StreamingMessage terminal = messages[^1];
        Assert.Equal(StatusResult.Types.Status.Success, terminal.ExtensionPayloadResponse.Result.Status);
        Assert.Equal(offset, terminal.ExtensionPayloadResponse.Offset);
        Assert.Single(messages.Where(message => message.ExtensionPayloadResponse.ContentCase is ExtensionPayloadResponse.ContentOneofCase.Result));
        bytes.Position = 0;
        using ZipArchive archive = new(bytes, ZipArchiveMode.Read);
        Assert.NotNull(archive.GetEntry("extensions.json"));
        Assert.NotNull(archive.GetEntry(".azurefunctions/payload.bin"));
        AssertNoTemporaryArchives();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InitializationAndReloadAdvertiseSupportWithoutStartingATransfer(bool reload)
    {
        using TestScopedEnvironmentVariable directory = new(ExtensionPayload.ApplicationDirectoryVariable, "relative");
        using RecordingClient client = new();
        TaskCompletionSource<StreamingMessage> initialized = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.OnMessage = message => initialized.TrySetResult(message);
        Mock<IWorkerClientFactory> factory = new();
        factory.Setup(value => value.CreateClient(It.IsAny<IMessageProcessor>())).Returns(client);
        GrpcWorker worker = new(Mock.Of<IFunctionsApplication>(), factory.Object, Mock.Of<IMethodInfoLocator>(), Options.Create(new WorkerOptions()),
            Mock.Of<IFunctionMetadataManager>(), new ApplicationLifetime(NullLogger<ApplicationLifetime>.Instance), Mock.Of<IInvocationHandler>(),
            CreateProducer(), NullLogger<GrpcWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        try
        {
            StreamingMessage request = new() { RequestId = "initialize" };
            if (reload)
            {
                request.FunctionEnvironmentReloadRequest = new();
            }
            else
            {
                request.WorkerInitRequest = new();
            }

            await ((IMessageProcessor)worker).ProcessMessageAsync(request);
            StreamingMessage response = await initialized.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Same(response, Assert.Single(client.Messages));
            Assert.Equal("initialize", response.RequestId);
            if (reload)
            {
                Assert.Equal(StreamingMessage.ContentOneofCase.FunctionEnvironmentReloadResponse, response.ContentCase);
                Assert.Equal(StatusResult.Types.Status.Success, response.FunctionEnvironmentReloadResponse.Result.Status);
                Assert.Equal("1", response.FunctionEnvironmentReloadResponse.Capabilities[WorkerCapabilities.ExtensionPayload]);
            }
            else
            {
                Assert.Equal(StreamingMessage.ContentOneofCase.WorkerInitResponse, response.ContentCase);
                Assert.Equal(StatusResult.Types.Status.Success, response.WorkerInitResponse.Result.Status);
                Assert.Equal("1", response.WorkerInitResponse.Capabilities[WorkerCapabilities.ExtensionPayload]);
            }

            AssertNoTemporaryArchives();
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OverlappingRequestsJoinDuplicatesRejectCompetitorsAndReleaseOwnership(bool failTerminalWrite)
    {
        WriteData(200 * 1024);
        ExtensionPayload producer = CreateProducer();
        using RecordingClient client = new();
        TaskCompletionSource<bool> firstChunk = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.OnPayload = async (message, _) =>
        {
            if (!string.Equals(message.RequestId, "active", StringComparison.Ordinal))
            {
                return;
            }

            if (message.ExtensionPayloadResponse.ContentCase is ExtensionPayloadResponse.ContentOneofCase.Chunk)
            {
                firstChunk.TrySetResult(true);
                await release.Task;
            }
            else if (failTerminalWrite)
            {
                throw new IOException("Simulated terminal write failure.");
            }
        };
        Task transfer = producer.SendAsync("active", client, CancellationToken.None);
        try
        {
            await firstChunk.Task.WaitAsync(TimeSpan.FromSeconds(15));
            string archive = Assert.Single(Directory.EnumerateFiles(_temporaryRoot));
            Task duplicate = producer.SendAsync("active", client, CancellationToken.None);
            Assert.Same(transfer, duplicate);
            Assert.False(duplicate.IsCompleted);
            Assert.Single(client.Messages);

            await producer.SendAsync("competing", client, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal(2, client.Messages.Count);
            StreamingMessage rejected = Assert.Single(client.Messages.Where(message => string.Equals(message.RequestId, "competing", StringComparison.Ordinal)));
            Assert.Equal(ExtensionPayloadResponse.ContentOneofCase.Result, rejected.ExtensionPayloadResponse.ContentCase);
            Assert.Equal(StatusResult.Types.Status.Failure, rejected.ExtensionPayloadResponse.Result.Status);
            Assert.Equal(0UL, rejected.ExtensionPayloadResponse.Offset);
            Assert.Equal(archive, Assert.Single(Directory.EnumerateFiles(_temporaryRoot)));
            Assert.False(transfer.IsCompleted);
        }
        finally
        {
            release.TrySetResult(true);
            if (failTerminalWrite)
            {
                await Assert.ThrowsAsync<IOException>(() => transfer).WaitAsync(TimeSpan.FromSeconds(15));
            }
            else
            {
                await transfer.WaitAsync(TimeSpan.FromSeconds(15));
            }
        }

        StreamingMessage[] activeResponses = client.Messages.Where(message => string.Equals(message.RequestId, "active", StringComparison.Ordinal)).ToArray();
        ulong offset = 0;
        foreach (StreamingMessage response in activeResponses.Take(activeResponses.Length - 1))
        {
            Assert.Equal(ExtensionPayloadResponse.ContentOneofCase.Chunk, response.ExtensionPayloadResponse.ContentCase);
            Assert.Equal(offset, response.ExtensionPayloadResponse.Offset);
            offset += (uint)response.ExtensionPayloadResponse.Chunk.Length;
        }

        StreamingMessage terminal = Assert.Single(activeResponses.Where(message => message.ExtensionPayloadResponse.ContentCase is ExtensionPayloadResponse.ContentOneofCase.Result));
        Assert.Same(activeResponses[^1], terminal);
        Assert.Equal(offset, terminal.ExtensionPayloadResponse.Offset);
        AssertNoTemporaryArchives();

        await producer.SendAsync("fresh", client, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(15));
        StreamingMessage[] freshResponses = client.Messages.Where(message => string.Equals(message.RequestId, "fresh", StringComparison.Ordinal)).ToArray();
        Assert.Contains(freshResponses, message => message.ExtensionPayloadResponse.ContentCase is ExtensionPayloadResponse.ContentOneofCase.Chunk);
        StreamingMessage completed = Assert.Single(freshResponses.Where(message => message.ExtensionPayloadResponse.ContentCase is ExtensionPayloadResponse.ContentOneofCase.Result));
        Assert.Equal(StatusResult.Types.Status.Success, completed.ExtensionPayloadResponse.Result.Status);
        AssertNoTemporaryArchives();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConnectionLossOrWorkerShutdownWaitsForInFlightWriteBeforeDeletingZip(bool connectionLost)
    {
        WriteData(200 * 1024);
        ExtensionPayload producer = CreateProducer();
        using RecordingClient client = new();
        using CancellationTokenSource stopping = new();
        TaskCompletionSource<CancellationToken> firstChunk = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.OnPayload = async (message, token) =>
        {
            if (message.ExtensionPayloadResponse.ContentCase is ExtensionPayloadResponse.ContentOneofCase.Chunk)
            {
                firstChunk.TrySetResult(token);
                await release.Task;
            }
        };
        Task transfer = producer.SendAsync("active", client, stopping.Token);
        try
        {
            CancellationToken token = await firstChunk.Task.WaitAsync(TimeSpan.FromSeconds(15));
            string archive = Assert.Single(Directory.EnumerateFiles(_temporaryRoot));
            Assert.False(token.IsCancellationRequested);
            if (connectionLost)
            {
                await client.StopAsync(CancellationToken.None);
            }
            else
            {
                stopping.Cancel();
            }

            Assert.True(token.IsCancellationRequested);
            Task drain = producer.CancelAsync();
            Assert.Same(transfer, drain);
            Assert.False(drain.IsCompleted);
            Assert.True(File.Exists(archive));
            Assert.Equal(archive, Assert.Single(Directory.EnumerateFiles(_temporaryRoot)));
            Assert.Single(client.Messages);
        }
        finally
        {
            stopping.Cancel();
            release.TrySetResult(true);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transfer).WaitAsync(TimeSpan.FromSeconds(15));
        }

        StreamingMessage chunk = Assert.Single(client.Messages);
        Assert.Equal(ExtensionPayloadResponse.ContentOneofCase.Chunk, chunk.ExtensionPayloadResponse.ContentCase);
        AssertNoTemporaryArchives();
    }

    [Fact]
    public async Task NativeTransportRejectsWithoutPreparingAnArchive()
    {
        using TestScopedEnvironmentVariable malformed = new(ExtensionPayload.ApplicationDirectoryVariable, "relative");
        GrpcHostChannel channel = new(System.Threading.Channels.Channel.CreateUnbounded<StreamingMessage>());
        NativeWorkerClient native = new(Mock.Of<IMessageProcessor>(), channel);
        Assert.False(native.SupportsExtensionPayload);
        await CreateProducer().SendAsync("native-request", native, CancellationToken.None);
        Assert.True(channel.Channel.Reader.TryRead(out StreamingMessage terminal));
        Assert.Equal(StatusResult.Types.Status.Failure, terminal.ExtensionPayloadResponse.Result.Status);
        Assert.False(channel.Channel.Reader.TryRead(out _));
        AssertNoTemporaryArchives();
    }

    private ExtensionPayload CreateProducer(int messageLimit = 4 * 1024 * 1024)
        => new(Options.Create(new GrpcWorkerStartupOptions { GrpcMaxMessageLength = messageLimit }), NullLogger<ExtensionPayload>.Instance);

    private void WriteData(int length)
    {
        byte[] data = new byte[length];
        new Random(23).NextBytes(data);
        File.WriteAllBytes(Path.Combine(_root, ".azurefunctions", "payload.bin"), data);
    }

    private void AssertPayloadSupportIsAdvertised()
    {
        WorkerInitResponse init = GrpcWorker.WorkerInitRequestHandler(new(), new());
        FunctionEnvironmentReloadResponse reload = GrpcWorker.EnvironmentReloadRequestHandler(new());
        Assert.Equal(StatusResult.Types.Status.Success, init.Result.Status);
        Assert.Equal(StatusResult.Types.Status.Success, reload.Result.Status);
        Assert.Equal("1", init.Capabilities[WorkerCapabilities.ExtensionPayload]);
        Assert.Equal("1", reload.Capabilities[WorkerCapabilities.ExtensionPayload]);
        AssertNoTemporaryArchives();
    }

    private void AssertNoTemporaryArchives() => Assert.Empty(Directory.EnumerateFiles(_temporaryRoot));

    private sealed class RecordingClient : IWorkerClient, IDisposable
    {
        private readonly CancellationTokenSource _closed = new();
        public ConcurrentQueue<StreamingMessage> Messages { get; } = new();
        public Func<StreamingMessage, CancellationToken, Task> OnPayload { get; set; }
        public Action<StreamingMessage> OnMessage { get; set; }
        public bool SupportsExtensionPayload => true;
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
}
