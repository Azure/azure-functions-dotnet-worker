// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.FunctionRpc;
using RpcException = Grpc.Core.RpcException;

namespace Microsoft.Azure.Functions.Worker.Tests;

public class GrpcWorkerClientStartupTests
{
    [Fact]
    public async Task ProductionClientDeliversStartStreamAfterListenerStarts()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        // Select a loopback port, then leave it unbound while the worker starts.
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        // Prepare an HTTP/2 endpoint without starting its listener.
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, port, endpoint => endpoint.Protocols = HttpProtocols.Http2));
        await using var server = builder.Build();
        var received = new TaskCompletionSource<StreamingMessage[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var processed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.MapPost("/AzureFunctionsRpcMessages.FunctionRpc/EventStream", async context =>
        {
            context.Response.ContentType = "application/grpc";
            // Each uncompressed gRPC frame has a one-byte flag and a four-byte big-endian payload length.
            // Read StartStream and the queued message directly from the real request stream.
            var messages = new StreamingMessage[2];
            for (int i = 0; i < messages.Length; i++)
            {
                byte[] header = new byte[5];
                await context.Request.Body.ReadExactlyAsync(header, context.RequestAborted);
                Assert.Equal(0, header[0]);
                byte[] payload = new byte[BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(1))];
                await context.Request.Body.ReadExactlyAsync(payload, context.RequestAborted);
                messages[i] = StreamingMessage.Parser.ParseFrom(payload);
            }

            received.TrySetResult(messages);
            // Send a framed response to exercise the worker's reader, keeping the duplex stream open until shutdown.
            byte[] response = new StreamingMessage { RequestId = "response" }.ToByteArray();
            byte[] frame = new byte[5 + response.Length];
            BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(1), response.Length);
            response.CopyTo(frame, 5);
            await context.Response.Body.WriteAsync(frame, context.RequestAborted);
            await context.Response.Body.FlushAsync(context.RequestAborted);
            await Task.Delay(Timeout.Infinite, context.RequestAborted);
        });

        // Use the production factory rather than injecting a separately configured channel or mocked RPC client.
        var output = new GrpcHostChannel(System.Threading.Channels.Channel.CreateUnbounded<StreamingMessage>());
        var processor = new Mock<IMessageProcessor>();
        processor.Setup(p => p.ProcessMessageAsync(It.IsAny<StreamingMessage>())).Returns((StreamingMessage message) =>
        {
            Assert.Equal("response", message.RequestId);
            processed.TrySetResult(true);
            return Task.CompletedTask;
        });
        var factory = new GrpcWorkerClientFactory(output, Options.Create(new GrpcWorkerStartupOptions
        {
            HostEndpoint = new Uri($"http://127.0.0.1:{port}"),
            WorkerId = "production-worker",
            GrpcMaxMessageLength = 1024 * 1024
        }), NullLogger<GrpcWorkerClientFactory>.Instance);
        var client = factory.CreateClient(processor.Object);
        // Check the actual transport configuration without adding a product API solely for test inspection.
        using var channel = (GrpcChannel)client.GetType().GetField("_grpcChannel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
        Assert.Equal(TimeSpan.FromMilliseconds(10), GetChannelOption(channel, "InitialReconnectBackoff"));
        Assert.Equal(TimeSpan.FromSeconds(10), GetChannelOption(channel, "MaxReconnectBackoff"));
        Assert.IsType<NullLoggerFactory>(GetChannelOption(channel, "LoggerFactory"));
        output.Channel.Writer.TryWrite(new StreamingMessage { RequestId = "queued" });

        Task startup = client.StartAsync(cancellation.Token);
        // Keep the port closed long enough for startup to encounter an unavailable listener.
        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellation.Token);
        Assert.False(startup.IsCompleted);
        Assert.False(received.Task.IsCompleted);
        // Start the listener and verify retries hand off to the real stream in both directions.
        await server.StartAsync(cancellation.Token);
        await startup.WaitAsync(cancellation.Token);
        StreamingMessage[] actual = await received.Task.WaitAsync(cancellation.Token);
        Assert.Equal("production-worker", actual[0].StartStream.WorkerId);
        Assert.Equal("queued", actual[1].RequestId);
        await processed.Task.WaitAsync(cancellation.Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.StartAsync(cancellation.Token));

        // End the stream and dispose its transport before stopping the server.
        cancellation.Cancel();
        await client.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));
        await server.StopAsync();
    }

    private static object GetChannelOption(GrpcChannel channel, string name)
        => typeof(GrpcChannel).GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(channel);

    [Fact]
    public async Task ProductionClientStopCancelsPendingConnection()
    {
        using var reservedPort = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        reservedPort.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        int port = ((IPEndPoint)reservedPort.LocalEndPoint).Port;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var output = new GrpcHostChannel(System.Threading.Channels.Channel.CreateUnbounded<StreamingMessage>());
        var factory = new GrpcWorkerClientFactory(output, Options.Create(new GrpcWorkerStartupOptions
        {
            HostEndpoint = new Uri($"http://127.0.0.1:{port}"),
            WorkerId = "stopped-worker",
            GrpcMaxMessageLength = 1024 * 1024
        }), NullLogger<GrpcWorkerClientFactory>.Instance);
        var client = factory.CreateClient(Mock.Of<IMessageProcessor>());
        try
        {
            Task startup = client.StartAsync(cancellation.Token);
            Assert.False(startup.IsCompleted);

            Task stopping = client.StopAsync(CancellationToken.None);
            Exception failure = await Record.ExceptionAsync(() => startup.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.True(failure is OperationCanceledException
                || failure is RpcException rpc && rpc.StatusCode == StatusCode.Cancelled);
            await stopping.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(client.ConnectionClosed.IsCancellationRequested);
            Assert.False(cancellation.IsCancellationRequested);
            Assert.False(output.Channel.Writer.TryWrite(new()));
        }
        finally
        {
            cancellation.Cancel();
            await client.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    [Fact]
    public async Task UngatedStartStreamFailsWhenListenerIsAbsent()
    {
        using var fixture = new ClientFixture();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var stream = new FunctionRpcClient(fixture.Channel).EventStream(cancellationToken: cancellation.Token);
        RpcException failure = await Assert.ThrowsAsync<RpcException>(() => stream.RequestStream.WriteAsync(
            new StreamingMessage { StartStream = new StartStream { WorkerId = "test-worker" } }));

        Assert.Equal(StatusCode.Unavailable, failure.StatusCode);
    }

    [Fact]
    public async Task WaitForReadyIsEnabledAndStartStreamIsSentFirstOnlyOnce()
    {
        using var fixture = new ClientFixture();
        fixture.PendingWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var queuedMessage = new StreamingMessage { RequestId = "queued" };
        fixture.Output.Channel.Writer.TryWrite(queuedMessage);
        fixture.Output.Channel.Writer.Complete();

        Task startup = fixture.Client.StartAsync(cancellation.Token);
        Assert.False(startup.IsCompleted);
        Assert.Equal(1, fixture.StreamCalls);
        Assert.True(fixture.StreamOptions.IsWaitForReady);
        Assert.Equal(fixture.Client.ConnectionClosed, fixture.StreamOptions.CancellationToken);
        Assert.Empty(fixture.Messages);

        fixture.PendingWrite.SetResult(true);
        await startup.WaitAsync(cancellation.Token);

        Assert.Equal(1, fixture.StreamCalls);
        Assert.Equal(2, fixture.Messages.Count);
        Assert.Equal("test-worker", fixture.Messages[0].StartStream.WorkerId);
        Assert.Same(queuedMessage, fixture.Messages[1]);

        // A completed response stream must not restart startup or replay StartStream.
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Client.StartAsync(cancellation.Token));
        Assert.Equal(1, fixture.StreamCalls);
    }

    [Fact]
    public async Task CancelingPendingStartStreamDisposesStreamAndChannel()
    {
        using var fixture = new ClientFixture();
        fixture.PendingWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        Task startup = fixture.Client.StartAsync(cancellation.Token);
        Assert.False(startup.IsCompleted);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => startup);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => fixture.Channel.ConnectAsync());
        Assert.Equal(1, fixture.StreamCalls);
        Assert.True(fixture.StreamDisposed);
        Assert.Empty(fixture.Messages);
    }

    [Fact]
    public async Task StoppingPendingStartupCancelsRetriesAndDisposesStreamAndChannel()
    {
        using var fixture = new ClientFixture();
        fixture.PendingWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        Task startup = fixture.Client.StartAsync(cancellation.Token);
        Assert.False(startup.IsCompleted);

        Task stopping = fixture.Client.StopAsync(CancellationToken.None);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => startup).WaitAsync(TimeSpan.FromSeconds(10));
        await stopping.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.False(cancellation.IsCancellationRequested);
        Assert.True(fixture.StreamOptions.CancellationToken.IsCancellationRequested);
        Assert.True(fixture.StreamOptions.IsWaitForReady);
        Assert.Equal(1, fixture.StreamCalls);
        Assert.True(fixture.StreamDisposed);
        Assert.Empty(fixture.Messages);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => fixture.Channel.ConnectAsync());
    }

    [Fact]
    public async Task AlreadyCanceledStartupDoesNotConnectOrOpenStream()
    {
        using var fixture = new ClientFixture();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Client.StartAsync(cancellation.Token));
        Assert.Equal(0, fixture.StreamCalls);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => fixture.Channel.ConnectAsync());
    }

    [Fact]
    public async Task StartStreamFailureIsNotRetriedAndDisposesStreamAndChannel()
    {
        using var fixture = new ClientFixture();
        var failure = new RpcException(new Status(StatusCode.Unavailable, "Stream failed after TCP readiness."));
        fixture.WriteFailure = failure;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        Exception actual = await Assert.ThrowsAsync<RpcException>(() => fixture.Client.StartAsync(cancellation.Token));
        Assert.Same(failure, actual);
        Assert.Equal(1, fixture.StreamCalls);
        Assert.True(fixture.StreamDisposed);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => fixture.Channel.ConnectAsync());
    }

    private sealed class ClientFixture : IDisposable
    {
        public TcpListener Listener { get; }
        public GrpcChannel Channel { get; }
        public GrpcHostChannel Output { get; } = new(System.Threading.Channels.Channel.CreateUnbounded<StreamingMessage>());
        public GrpcWorkerClientFactory.GrpcWorkerClient Client { get; }
        public List<StreamingMessage> Messages { get; } = [];
        public int StreamCalls { get; private set; }
        public bool StreamDisposed { get; private set; }
        public Exception WriteFailure { get; set; }
        public TaskCompletionSource<bool> PendingWrite { get; set; }
        public CallOptions StreamOptions { get; private set; }

        public ClientFixture()
        {
            Listener = new TcpListener(IPAddress.Loopback, 0);
            Listener.Start();
            int port = ((IPEndPoint)Listener.LocalEndpoint).Port;
            Listener.Stop();
            Listener = new TcpListener(IPAddress.Loopback, port);
            Channel = GrpcChannel.ForAddress($"http://127.0.0.1:{port}");
            var writer = new Mock<IClientStreamWriter<StreamingMessage>>();
            writer.Setup(stream => stream.WriteAsync(It.IsAny<StreamingMessage>())).Returns(async (StreamingMessage message) =>
            {
                if (PendingWrite is not null)
                {
                    await PendingWrite.Task.WaitAsync(StreamOptions.CancellationToken);
                }

                if (WriteFailure is not null)
                {
                    throw WriteFailure;
                }

                Messages.Add(message);
            });
            var reader = new Mock<IAsyncStreamReader<StreamingMessage>>();
            reader.Setup(stream => stream.MoveNext(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            var rpc = new Mock<FunctionRpcClient>();
            rpc.Setup(client => client.EventStream(It.IsAny<CallOptions>())).Returns((CallOptions options) =>
            {
                StreamOptions = options;
                StreamCalls++;
                return new AsyncDuplexStreamingCall<StreamingMessage, StreamingMessage>(writer.Object, reader.Object, Task.FromResult(new Metadata()),
                    () => Status.DefaultSuccess, () => new Metadata(), () => StreamDisposed = true);
            });
            Client = new(Output, new GrpcWorkerStartupOptions { WorkerId = "test-worker" }, Mock.Of<IMessageProcessor>(),
                NullLogger<GrpcWorkerClientFactory>.Instance, Channel, rpc.Object);
        }

        public void Dispose()
        {
            Output.Channel.Writer.TryComplete();
            Channel.Dispose();
            Listener.Stop();
        }
    }
}
