// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Testing.Protocol;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Testing.Tests.Protocol;

public class InMemoryFunctionsHostProtocolTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task Protocol_InitializationUsesRequiredOrderAndExactMetadata()
    {
        await using ProtocolHarness harness = ProtocolHarness.Create();

        await harness.Host.InitializeAsync(@"c:\functions", TestTimeout);

        Assert.Equal(InMemoryFunctionsHostState.Ready, harness.Host.State);
        Assert.Collection(
            harness.Processor.Requests,
            request => Assert.Equal(StreamingMessage.ContentOneofCase.WorkerInitRequest, request.ContentCase),
            request => Assert.Equal(StreamingMessage.ContentOneofCase.FunctionsMetadataRequest, request.ContentCase),
            request =>
            {
                Assert.Equal(StreamingMessage.ContentOneofCase.FunctionLoadRequest, request.ContentCase);
                Assert.Equal("function-1", request.FunctionLoadRequest.FunctionId);
                Assert.Equal("TestFunction", request.FunctionLoadRequest.Metadata.Name);
            });
        Assert.Single(harness.Host.FunctionMetadata);
    }

    [Fact]
    public async Task Protocol_MissingConnectionHonorsStartupTimeout()
    {
        await using var host = new InMemoryFunctionsHost(TestTimeout, 1024 * 1024);

        TimeoutException exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            host.InitializeAsync(@"c:\functions", TimeSpan.FromMilliseconds(100)));

        Assert.Contains("connect", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Protocol_DuplicateFunctionMetadataFaultsStartup()
    {
        await using ProtocolHarness harness = ProtocolHarness.Create(request =>
        {
            StreamingMessage response = ProtocolHarness.CreateSuccessfulResponse(request);
            if (request.FunctionsMetadataRequest is not null)
            {
                response.FunctionMetadataResponse.FunctionMetadataResults.Add(
                    response.FunctionMetadataResponse.FunctionMetadataResults[0].Clone());
            }

            return response;
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Host.InitializeAsync(@"c:\functions", TestTimeout));
        Assert.Equal(InMemoryFunctionsHostState.Faulted, harness.Host.State);
    }

    [Fact]
    public async Task Protocol_FailedInitializationStatusFaultsStartup()
    {
        await using ProtocolHarness harness = ProtocolHarness.Create(request =>
        {
            StreamingMessage response = ProtocolHarness.CreateSuccessfulResponse(request);
            if (request.WorkerInitRequest is not null)
            {
                response.WorkerInitResponse.Result = Failure("init failed");
            }

            return response;
        });

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Host.InitializeAsync(@"c:\functions", TestTimeout));

        Assert.Contains("init failed", exception.Message, StringComparison.Ordinal);
        Assert.Equal(InMemoryFunctionsHostState.Faulted, harness.Host.State);
    }

    [Fact]
    public async Task Protocol_DuplicateActiveInvocationIdIsRejected()
    {
        var invocationSeen = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseInvocation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ProtocolHarness harness = ProtocolHarness.Create(async request =>
        {
            if (request.InvocationRequest is not null)
            {
                invocationSeen.SetResult();
                await releaseInvocation.Task;
            }

            return ProtocolHarness.CreateSuccessfulResponse(request);
        });
        await harness.Host.InitializeAsync(@"c:\functions", TestTimeout);
        var invocation = new InvocationRequest { InvocationId = "invocation-1", FunctionId = "function-1" };

        Task<InvocationResponse> first = harness.Host.InvokeAsync(invocation, TestTimeout);
        await invocationSeen.Task.WaitAsync(TestTimeout);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Host.InvokeAsync(invocation.Clone(), TestTimeout));

        releaseInvocation.SetResult();
        InvocationResponse response = await first;
        Assert.Equal("invocation-1", response.InvocationId);
    }

    [Fact]
    public async Task Protocol_CancellationIsIdempotentAndReturnsCancelledInvocation()
    {
        var invocationSeen = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationSeen = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ProtocolHarness harness = ProtocolHarness.Create(async request =>
        {
            if (request.InvocationRequest is not null)
            {
                invocationSeen.SetResult();
                await cancellationSeen.Task;
                return new StreamingMessage
                {
                    InvocationResponse = new InvocationResponse
                    {
                        InvocationId = request.InvocationRequest.InvocationId,
                        Result = Status(StatusResult.Types.Status.Cancelled)
                    }
                };
            }

            if (request.InvocationCancel is not null)
            {
                cancellationSeen.SetResult();
            }

            return ProtocolHarness.CreateSuccessfulResponse(request);
        });
        await harness.Host.InitializeAsync(@"c:\functions", TestTimeout);

        Task<InvocationResponse> invocation = harness.Host.InvokeAsync(
            new InvocationRequest { InvocationId = "cancel-me", FunctionId = "function-1" },
            TestTimeout);
        await invocationSeen.Task.WaitAsync(TestTimeout);
        await harness.Host.CancelInvocationAsync("cancel-me", TestTimeout);

        Assert.Equal(StatusResult.Types.Status.Cancelled, (await invocation).Result.Status);
        int cancellationCount = harness.Processor.Requests.Count(request => request.InvocationCancel is not null);
        await harness.Host.CancelInvocationAsync("cancel-me", TestTimeout);
        Assert.Equal(cancellationCount, harness.Processor.Requests.Count(request => request.InvocationCancel is not null));
    }

    [Fact]
    public async Task Protocol_InvocationTimeoutRemovesActiveIdAndAllowsReuse()
    {
        int invocationCount = 0;
        await using ProtocolHarness harness = ProtocolHarness.Create(request =>
        {
            if (request.InvocationRequest is not null && Interlocked.Increment(ref invocationCount) == 1)
            {
                return null;
            }

            return ProtocolHarness.CreateSuccessfulResponse(request);
        });
        await harness.Host.InitializeAsync(@"c:\functions", TestTimeout);
        var request = new InvocationRequest { InvocationId = "reusable", FunctionId = "function-1" };

        await Assert.ThrowsAsync<TimeoutException>(() =>
            harness.Host.InvokeAsync(request, TimeSpan.FromMilliseconds(50)));
        InvocationResponse response = await harness.Host.InvokeAsync(request.Clone(), TestTimeout);

        Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
    }

    [Fact]
    public async Task Protocol_LogChannelIsCapturedWithoutNetworkTransport()
    {
        await using ProtocolHarness harness = ProtocolHarness.Create();
        await harness.Host.InitializeAsync(@"c:\functions", TestTimeout);

        await harness.Channel.Channel.Writer.WriteAsync(new StreamingMessage
        {
            RpcLog = new RpcLog { Message = "captured log" }
        });

        await WaitUntilAsync(() => harness.Host.Logs.Count == 1, TestTimeout);
        Assert.Equal("captured log", Assert.Single(harness.Host.Logs).Message);
    }

    [Fact]
    public async Task Protocol_MessageLengthLimitFaultsStartup()
    {
        await using ProtocolHarness harness = ProtocolHarness.Create(maximumMessageLength: 16);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Host.InitializeAsync(@"c:\a-directory-longer-than-the-limit", TestTimeout));
        Assert.Equal(InMemoryFunctionsHostState.Faulted, harness.Host.State);
    }

    [Fact]
    public async Task Protocol_DisposeTerminatesAndIsIdempotent()
    {
        ProtocolHarness harness = ProtocolHarness.Create();
        await harness.Host.InitializeAsync(@"c:\functions", TestTimeout);

        await harness.Host.DisposeAsync();
        await harness.Host.DisposeAsync();

        Assert.Equal(InMemoryFunctionsHostState.Stopped, harness.Host.State);
        Assert.Single(harness.Processor.Requests.Where(request => request.WorkerTerminate is not null));
        await harness.Factory.DisposeAsync();
    }

    [Fact]
    public async Task Protocol_SecondClientStartIsRejected()
    {
        await using ProtocolHarness harness = ProtocolHarness.Create();

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Client.StartAsync(CancellationToken.None));
    }

    private static StatusResult Status(StatusResult.Types.Status status)
        => new() { Status = status };

    private static StatusResult Failure(string message)
        => new()
        {
            Status = StatusResult.Types.Status.Failure,
            Exception = new RpcException { Message = message }
        };

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        while (!condition())
        {
            await Task.Delay(10, cancellation.Token);
        }
    }

    private sealed class ProtocolHarness : IAsyncDisposable
    {
        private ProtocolHarness(
            InMemoryFunctionsHost host,
            InMemoryWorkerClientFactory factory,
            ScriptedMessageProcessor processor,
            IWorkerClient client,
            GrpcHostChannel channel)
        {
            Host = host;
            Factory = factory;
            Processor = processor;
            Client = client;
            Channel = channel;
        }

        internal InMemoryFunctionsHost Host { get; }

        internal InMemoryWorkerClientFactory Factory { get; }

        internal ScriptedMessageProcessor Processor { get; }

        internal IWorkerClient Client { get; }

        internal GrpcHostChannel Channel { get; }

        internal static ProtocolHarness Create(
            Func<StreamingMessage, StreamingMessage?>? handler = null,
            int maximumMessageLength = 1024 * 1024)
        {
            handler ??= CreateSuccessfulResponse;
            return Create(request => Task.FromResult(handler(request)), maximumMessageLength);
        }

        internal static ProtocolHarness Create(
            Func<StreamingMessage, Task<StreamingMessage?>> handler,
            int maximumMessageLength = 1024 * 1024)
        {
            var host = new InMemoryFunctionsHost(TestTimeout, maximumMessageLength);
            var channel = new GrpcHostChannel(System.Threading.Channels.Channel.CreateUnbounded<StreamingMessage>());
            var factory = new InMemoryWorkerClientFactory(host, channel);
            var processor = new ScriptedMessageProcessor(handler);
            IWorkerClient client = factory.CreateClient(processor);
            processor.Client = client;
            client.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
            return new ProtocolHarness(host, factory, processor, client, channel);
        }

        internal static StreamingMessage CreateSuccessfulResponse(StreamingMessage request)
        {
            var response = new StreamingMessage();
            switch (request.ContentCase)
            {
                case StreamingMessage.ContentOneofCase.WorkerInitRequest:
                    response.WorkerInitResponse = new WorkerInitResponse { Result = Status(StatusResult.Types.Status.Success) };
                    break;
                case StreamingMessage.ContentOneofCase.FunctionsMetadataRequest:
                    response.FunctionMetadataResponse = new FunctionMetadataResponse { Result = Status(StatusResult.Types.Status.Success) };
                    response.FunctionMetadataResponse.FunctionMetadataResults.Add(new RpcFunctionMetadata
                    {
                        FunctionId = "function-1",
                        Name = "TestFunction",
                        EntryPoint = "Functions.TestFunction"
                    });
                    break;
                case StreamingMessage.ContentOneofCase.FunctionLoadRequest:
                    response.FunctionLoadResponse = new FunctionLoadResponse
                    {
                        FunctionId = request.FunctionLoadRequest.FunctionId,
                        Result = Status(StatusResult.Types.Status.Success)
                    };
                    break;
                case StreamingMessage.ContentOneofCase.InvocationRequest:
                    response.InvocationResponse = new InvocationResponse
                    {
                        InvocationId = request.InvocationRequest.InvocationId,
                        Result = Status(StatusResult.Types.Status.Success)
                    };
                    break;
            }

            return response;
        }

        public async ValueTask DisposeAsync()
        {
            await Host.DisposeAsync();
            await Factory.DisposeAsync();
        }
    }

    private sealed class ScriptedMessageProcessor : IMessageProcessor
    {
        private readonly Func<StreamingMessage, Task<StreamingMessage?>> _handler;

        internal ScriptedMessageProcessor(Func<StreamingMessage, Task<StreamingMessage?>> handler)
        {
            _handler = handler;
        }

        internal ConcurrentQueue<StreamingMessage> Requests { get; } = new();

        internal IWorkerClient? Client { get; set; }

        public Task ProcessMessageAsync(StreamingMessage request)
        {
            Requests.Enqueue(request.Clone());
            _ = RespondAsync(request.Clone());
            return Task.CompletedTask;
        }

        private async Task RespondAsync(StreamingMessage request)
        {
            StreamingMessage? response = await _handler(request);
            if (response is not null)
            {
                response.RequestId = request.RequestId;
                await Client!.SendMessageAsync(response);
            }
        }
    }

}
