// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Tests;
using Microsoft.Azure.Functions.Worker.Core.Configuration;
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
    private readonly TestScopedAppContextBaseDirectory _baseDirectory;
    private readonly TestScopedEnvironmentVariable[] _environment;
    private readonly ExtensionPayloadProvider _provider = new();
    private readonly HostConfigurationSnapshotProvider _hostConfigurationProvider = new(new PhysicalHostConfigurationFileSystem());

    public ExtensionPayloadTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ExtensionPayloadTests-" + Guid.NewGuid().ToString("N"));
        _temporaryRoot = Path.Combine(_root, "temporary");
        Directory.CreateDirectory(_temporaryRoot);
        Directory.CreateDirectory(Path.Combine(_root, ".azurefunctions"));
        File.WriteAllText(Path.Combine(_root, "extensions.json"), Manifest);
        _baseDirectory = new(_root + Path.DirectorySeparatorChar);
        _environment =
        [
            new("TMP", _temporaryRoot),
            new("TEMP", _temporaryRoot),
            new("TMPDIR", _temporaryRoot),
        ];
    }

    public void Dispose()
    {
        _baseDirectory.Dispose();
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
        using (Stream stream = await _provider.OpenReadAsync(CancellationToken.None))
        {
            temporary = Assert.Single(Directory.EnumerateFiles(_temporaryRoot, "*.zip", SearchOption.AllDirectories));
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
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PayloadUsesBaseDirectoryRegardlessOfEnvironment(bool trailingSeparator, bool legacyConfigured)
    {
        string legacyDirectory = Path.Combine(_root, "legacy-application");
        Directory.CreateDirectory(Path.Combine(legacyDirectory, ".azurefunctions"));
        File.WriteAllText(Path.Combine(legacyDirectory, "extensions.json"), "{\"extensions\":[]}");
        using TestScopedEnvironmentVariable legacy = new("FUNCTIONS_APPLICATION_DIRECTORY", legacyConfigured ? legacyDirectory : null);
        using TestScopedAppContextBaseDirectory application = new(trailingSeparator ? _root + Path.DirectorySeparatorChar : _root);
        using (Stream stream = await _provider.OpenReadAsync(CancellationToken.None))
        using (ZipArchive archive = new(stream, ZipArchiveMode.Read))
        {
            Assert.NotNull(archive.GetEntry(".azurefunctions/"));
            Assert.All(archive.Entries, entry => Assert.False(entry.FullName.StartsWith("/", StringComparison.Ordinal)));
            using StreamReader reader = new(archive.GetEntry("extensions.json")!.Open());
            Assert.Equal(Manifest, await reader.ReadToEndAsync());
        }

        AssertNoTemporaryArchives();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CapabilityIsSetOnceFromResolvedClient(bool supported)
    {
        using TestScopedAppContextBaseDirectory directory = new(Path.Combine(_root, "does-not-exist"));
        WorkerOptions options = new();
        options.Capabilities[WorkerCapabilities.ExtensionPayload] = "application override";
        using RecordingWorkerClient client = new() { SupportsExtensionPayload = supported };
        GrpcWorker worker = CreateWorker(client, options);
        await worker.StartAsync(CancellationToken.None);
        try
        {
            WorkerInitResponse init = GrpcWorker.WorkerInitRequestHandler(new(), options, _hostConfigurationProvider);
            FunctionEnvironmentReloadResponse reload = GrpcWorker.EnvironmentReloadRequestHandler(options);
            Assert.Equal(StatusResult.Types.Status.Success, init.Result.Status);
            Assert.Equal(StatusResult.Types.Status.Success, reload.Result.Status);
            Assert.Equal(supported, options.Capabilities.ContainsKey(WorkerCapabilities.ExtensionPayload));
            Assert.Equal(supported, init.Capabilities.ContainsKey(WorkerCapabilities.ExtensionPayload));
            Assert.Equal(supported, reload.Capabilities.ContainsKey(WorkerCapabilities.ExtensionPayload));
            if (supported)
            {
                Assert.Equal("1", init.Capabilities[WorkerCapabilities.ExtensionPayload]);
                Assert.Equal("1", reload.Capabilities[WorkerCapabilities.ExtensionPayload]);
            }

            AssertNoTemporaryArchives();
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Theory]
    [InlineData("manifest")]
    [InlineData("payload")]
    public async Task MissingInputsAreValidatedOnlyWhenPayloadIsRequested(string missing)
    {
        if (string.Equals(missing, "manifest", StringComparison.Ordinal))
        {
            File.Delete(Path.Combine(_root, "extensions.json"));
        }
        else if (string.Equals(missing, "payload", StringComparison.Ordinal))
        {
            Directory.Delete(Path.Combine(_root, ".azurefunctions"));
        }

        await AssertPayloadSupportIsAdvertisedAsync();
        await AssertPayloadRequestFailsAsync();
    }

    [Fact]
    public async Task MissingApplicationDirectoryIsValidatedOnlyWhenPayloadIsRequested()
    {
        using TestScopedAppContextBaseDirectory directory = new(Path.Combine(_root, "does-not-exist"));
        await AssertPayloadSupportIsAdvertisedAsync();
        await AssertPayloadRequestFailsAsync();
    }

    [Fact]
    public async Task NonDirectoryRootIsValidatedOnlyWhenPayloadIsRequested()
    {
        string path = Path.Combine(_root, "not-a-directory");
        File.WriteAllText(path, "file");
        using TestScopedAppContextBaseDirectory directory = new(path);
        await AssertPayloadSupportIsAdvertisedAsync();
        await AssertPayloadRequestFailsAsync();
    }

    [Fact]
    public async Task UnreadableManifestIsValidatedOnlyWhenPayloadIsRequested()
    {
        using FileStream held = File.Open(Path.Combine(_root, "extensions.json"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        await AssertPayloadSupportIsAdvertisedAsync();
        await AssertPayloadRequestFailsAsync();
    }

    [Theory]
    [InlineData(1024)]
    [InlineData(65536)]
    [InlineData(4194304)]
    public async Task StreamsCorrelatedBoundedChunksAndExactlyOneTerminal(int messageLimit)
    {
        WriteData(200 * 1024);
        using RecordingWorkerClient client = new();
        await CreateHandler(messageLimit).SendAsync("payload-operation", client, CancellationToken.None);
        StreamingMessage[] messages = client.Messages.ToArray();
        Assert.True(messages.Length > 2);
        ulong offset = 0;
        using MemoryStream bytes = new();
        foreach (StreamingMessage message in messages.Take(messages.Length - 1))
        {
            Assert.Equal("payload-operation", message.RequestId);
            Assert.Equal(ExtensionPayloadResponse.ContentOneofCase.Chunk, message.ExtensionPayloadResponse.ContentCase);
            Assert.InRange(message.ExtensionPayloadResponse.Chunk.Length, 1, ExtensionPayloadLimits.MaximumChunkBytes);
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
        using TestScopedAppContextBaseDirectory directory = new(Path.Combine(_root, "does-not-exist"));
        using RecordingWorkerClient client = new();
        TaskCompletionSource<StreamingMessage> initialized = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.OnMessage = message => initialized.TrySetResult(message);
        GrpcWorker worker = CreateWorker(client, new());
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
        ExtensionPayloadHandler handler = CreateHandler();
        using RecordingWorkerClient client = new();
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
        Task transfer = handler.SendAsync("active", client, CancellationToken.None);
        try
        {
            await firstChunk.Task.WaitAsync(TimeSpan.FromSeconds(15));
            string archive = Assert.Single(Directory.EnumerateFiles(_temporaryRoot, "*.zip", SearchOption.AllDirectories));
            Task duplicate = handler.SendAsync("active", client, CancellationToken.None);
            Assert.Same(transfer, duplicate);
            Assert.False(duplicate.IsCompleted);
            Assert.Single(client.Messages);

            await handler.SendAsync("competing", client, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal(2, client.Messages.Count);
            StreamingMessage rejected = Assert.Single(client.Messages.Where(message => string.Equals(message.RequestId, "competing", StringComparison.Ordinal)));
            Assert.Equal(ExtensionPayloadResponse.ContentOneofCase.Result, rejected.ExtensionPayloadResponse.ContentCase);
            Assert.Equal(StatusResult.Types.Status.Failure, rejected.ExtensionPayloadResponse.Result.Status);
            Assert.Equal(0UL, rejected.ExtensionPayloadResponse.Offset);
            Assert.Equal(archive, Assert.Single(Directory.EnumerateFiles(_temporaryRoot, "*.zip", SearchOption.AllDirectories)));
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

        await handler.SendAsync("fresh", client, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(15));
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
        ExtensionPayloadHandler handler = CreateHandler();
        using RecordingWorkerClient client = new();
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
        Task transfer = handler.SendAsync("active", client, stopping.Token);
        try
        {
            CancellationToken token = await firstChunk.Task.WaitAsync(TimeSpan.FromSeconds(15));
            string archive = Assert.Single(Directory.EnumerateFiles(_temporaryRoot, "*.zip", SearchOption.AllDirectories));
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
            Task drain = handler.CancelAsync();
            Assert.Same(transfer, drain);
            Assert.False(drain.IsCompleted);
            Assert.True(File.Exists(archive));
            Assert.Equal(archive, Assert.Single(Directory.EnumerateFiles(_temporaryRoot, "*.zip", SearchOption.AllDirectories)));
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
        using TestScopedAppContextBaseDirectory missing = new(Path.Combine(_root, "does-not-exist"));
        GrpcHostChannel channel = new(System.Threading.Channels.Channel.CreateUnbounded<StreamingMessage>());
        NativeWorkerClient native = new(Mock.Of<IMessageProcessor>(), channel);
        Assert.False(native.SupportsExtensionPayload);
        await CreateHandler().SendAsync("native-request", native, CancellationToken.None);
        Assert.True(channel.Channel.Reader.TryRead(out StreamingMessage terminal));
        Assert.Equal(StatusResult.Types.Status.Failure, terminal.ExtensionPayloadResponse.Result.Status);
        Assert.False(channel.Channel.Reader.TryRead(out _));
        AssertNoTemporaryArchives();
    }

    private ExtensionPayloadHandler CreateHandler(int messageLimit = 4 * 1024 * 1024)
        => new(_provider, Options.Create(new GrpcWorkerStartupOptions { GrpcMaxMessageLength = messageLimit }), NullLogger<ExtensionPayloadHandler>.Instance);

    private void WriteData(int length)
    {
        byte[] data = new byte[length];
        new Random(23).NextBytes(data);
        File.WriteAllBytes(Path.Combine(_root, ".azurefunctions", "payload.bin"), data);
    }

    private GrpcWorker CreateWorker(IWorkerClient client, WorkerOptions options)
    {
        Mock<IWorkerClientFactory> factory = new();
        factory.Setup(value => value.CreateClient(It.IsAny<IMessageProcessor>())).Returns(client);
        return new GrpcWorker(Mock.Of<IFunctionsApplication>(), factory.Object, Mock.Of<IMethodInfoLocator>(), Options.Create(options),
            Mock.Of<IFunctionMetadataManager>(), new ApplicationLifetime(NullLogger<ApplicationLifetime>.Instance), Mock.Of<IInvocationHandler>(),
            _hostConfigurationProvider, CreateHandler(), NullLogger<GrpcWorker>.Instance);
    }

    private async Task AssertPayloadSupportIsAdvertisedAsync()
    {
        WorkerOptions options = new();
        using RecordingWorkerClient client = new();
        GrpcWorker worker = CreateWorker(client, options);
        await worker.StartAsync(CancellationToken.None);
        try
        {
            WorkerInitResponse init = GrpcWorker.WorkerInitRequestHandler(new(), options, _hostConfigurationProvider);
            FunctionEnvironmentReloadResponse reload = GrpcWorker.EnvironmentReloadRequestHandler(options);
            Assert.Equal(StatusResult.Types.Status.Success, init.Result.Status);
            Assert.Equal(StatusResult.Types.Status.Success, reload.Result.Status);
            Assert.Equal("1", init.Capabilities[WorkerCapabilities.ExtensionPayload]);
            Assert.Equal("1", reload.Capabilities[WorkerCapabilities.ExtensionPayload]);
            AssertNoTemporaryArchives();
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    private void AssertNoTemporaryArchives() => Assert.Empty(Directory.EnumerateFileSystemEntries(_temporaryRoot));
}
