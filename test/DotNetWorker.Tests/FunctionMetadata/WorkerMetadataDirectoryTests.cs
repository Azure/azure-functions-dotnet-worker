// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

namespace Microsoft.Azure.Functions.Worker.Tests;

public sealed class WorkerMetadataDirectoryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "worker-metadata-directory-" + Guid.NewGuid().ToString("N"));

    public WorkerMetadataDirectoryTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "worker"));
        Directory.CreateDirectory(Path.Combine(_root, "host"));
        WriteMetadata(Path.Combine(_root, "worker"), "WorkerOnly");
        WriteMetadata(Path.Combine(_root, "host"), "HostOnly");
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Theory]
    [InlineData(true, "WorkerOnly")]
    [InlineData(false, "HostOnly")]
    public async Task FileProviderUsesConfiguredWorkerRootOrPreservesLegacyDirectory(bool configured, string expected)
    {
        using TestScopedEnvironmentVariable application = new(WorkerApplicationDirectory.EnvironmentVariable,
            configured ? Path.Combine(_root, "worker") : null);
        FunctionMetadataResponse response = await RequestAsync();
        Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
        Assert.False(response.UseDefaultMetadataIndexing);
        Assert.Equal(expected, Assert.Single(response.FunctionMetadataResults).Name);
        Assert.False(File.Exists(Path.Combine(_root, "worker", "extensions.json")));
    }

    [Theory]
    [InlineData("relative")]
    [InlineData("C:relative")]
    [InlineData("\\rooted")]
    [InlineData(" ")]
    public async Task MalformedRootDoesNotFallBackToHostFiles(string root)
    {
        using TestScopedEnvironmentVariable application = new(WorkerApplicationDirectory.EnvironmentVariable, root);
        FunctionMetadataResponse response = await RequestAsync();
        Assert.Equal(StatusResult.Types.Status.Failure, response.Result.Status);
        Assert.Empty(response.FunctionMetadataResults);
    }

    private async Task<FunctionMetadataResponse> RequestAsync()
    {
        MetadataClient client = new();
        Mock<IWorkerClientFactory> factory = new();
        factory.Setup(value => value.CreateClient(It.IsAny<IMessageProcessor>())).Returns(client);
        DefaultFunctionMetadataManager metadata = new(new DefaultFunctionMetadataProvider(), [], NullLogger<DefaultFunctionMetadataManager>.Instance);
        GrpcWorker worker = new(Mock.Of<IFunctionsApplication>(), factory.Object, Mock.Of<IMethodInfoLocator>(), Options.Create(new WorkerOptions()),
            metadata, new ApplicationLifetime(NullLogger<ApplicationLifetime>.Instance), Mock.Of<IInvocationHandler>(),
            new ExtensionPayload(Options.Create(new GrpcWorkerStartupOptions { GrpcMaxMessageLength = 4 * 1024 * 1024 }), NullLogger<ExtensionPayload>.Instance),
            NullLogger<GrpcWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        try
        {
            await ((IMessageProcessor)worker).ProcessMessageAsync(new()
            {
                RequestId = "metadata-request",
                FunctionsMetadataRequest = new() { FunctionAppDirectory = Path.Combine(_root, "host") }
            });
            StreamingMessage message = await client.Response.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal("metadata-request", message.RequestId);
            return message.FunctionMetadataResponse;
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    private static void WriteMetadata(string directory, string name)
        => File.WriteAllText(Path.Combine(directory, "functions.metadata"), $$"""
            [{"name":"{{name}}","scriptFile":"app.dll","entryPoint":"Functions.Queue.Run",
              "bindings":[{"name":"message","direction":"In","type":"queueTrigger","queueName":"items"}]}]
            """);

    private sealed class MetadataClient : IWorkerClient
    {
        public TaskCompletionSource<StreamingMessage> Response { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool SupportsExtensionPayload => true;
        public CancellationToken ConnectionClosed => CancellationToken.None;
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public ValueTask SendPayloadMessageAsync(StreamingMessage message, CancellationToken cancellationToken)
            => throw new NotSupportedException("This metadata fixture does not transfer extensions.");

        public ValueTask SendMessageAsync(StreamingMessage message)
        {
            Response.TrySetResult(message);
            return ValueTask.CompletedTask;
        }
    }
}
