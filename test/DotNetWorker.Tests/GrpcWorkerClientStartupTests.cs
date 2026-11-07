// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.FunctionRpc;
using RpcException = Grpc.Core.RpcException;

namespace Microsoft.Azure.Functions.Worker.Tests;

public class GrpcWorkerClientStartupTests
{
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
    public async Task BuiltInReconnectWaitsForDelayedListenerAndSendsStartStreamFirstOnlyOnce()
    {
        using var diagnostics = new ConnectionDiagnostics();
        using var fixture = new ClientFixture(diagnostics);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var queuedMessage = new StreamingMessage { RequestId = "queued" };
        fixture.Output.Channel.Writer.TryWrite(queuedMessage);
        fixture.Output.Channel.Writer.Complete();

        Task startup = fixture.Client.StartAsync(cancellation.Token);
        await diagnostics.FirstFailure.Task.WaitAsync(cancellation.Token);
        Assert.False(startup.IsCompleted);
        Assert.Equal(0, fixture.StreamCalls);
        Assert.Empty(fixture.Messages);

        fixture.Listener.Start();
        using TcpClient connection = await fixture.Listener.AcceptTcpClientAsync(cancellation.Token);
        await startup.WaitAsync(cancellation.Token);

        Assert.Equal(1, fixture.StreamCalls);
        Assert.Equal(2, fixture.Messages.Count);
        Assert.Equal("test-worker", fixture.Messages[0].StartStream.WorkerId);
        Assert.Same(queuedMessage, fixture.Messages[1]);
        Assert.True(diagnostics.Attempts.Count >= 2, "The real gRPC transport must retry the socket connection.");

        // A completed response stream must not restart startup or replay StartStream.
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Client.StartAsync(cancellation.Token));
        Assert.Equal(1, fixture.StreamCalls);
    }

    [Fact]
    public async Task CancelingConnectionWaitDisposesChannelAndNeverOpensStream()
    {
        using var diagnostics = new ConnectionDiagnostics();
        using var fixture = new ClientFixture(diagnostics);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        Task startup = fixture.Client.StartAsync(cancellation.Token);
        await diagnostics.FirstFailure.Task.WaitAsync(cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => startup);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => fixture.Channel.ConnectAsync());
        Assert.Equal(0, fixture.StreamCalls);
        Assert.Empty(fixture.Messages);
    }

    [Fact]
    public async Task AlreadyCanceledStartupDoesNotConnectOrOpenStream()
    {
        using var diagnostics = new ConnectionDiagnostics();
        using var fixture = new ClientFixture(diagnostics);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Client.StartAsync(cancellation.Token));
        Assert.Empty(diagnostics.Attempts);
        Assert.Equal(0, fixture.StreamCalls);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => fixture.Channel.ConnectAsync());
    }

    [Fact]
    public async Task StartStreamFailureIsNotRetriedAndDisposesStreamAndChannel()
    {
        using var fixture = new ClientFixture();
        fixture.Listener.Start();
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
        private readonly ILoggerFactory _loggerFactory;

        public ClientFixture(ILoggerProvider provider = null)
        {
            Listener = new TcpListener(IPAddress.Loopback, 0);
            Listener.Start();
            int port = ((IPEndPoint)Listener.LocalEndpoint).Port;
            Listener.Stop();
            Listener = new TcpListener(IPAddress.Loopback, port);
            _loggerFactory = provider is null ? NullLoggerFactory.Instance : LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(provider));
            Channel = GrpcChannel.ForAddress($"http://127.0.0.1:{port}", new GrpcChannelOptions { LoggerFactory = _loggerFactory });
            var writer = new Mock<IClientStreamWriter<StreamingMessage>>();
            writer.Setup(stream => stream.WriteAsync(It.IsAny<StreamingMessage>())).Returns((StreamingMessage message) =>
            {
                if (WriteFailure is not null)
                {
                    return Task.FromException(WriteFailure);
                }

                Messages.Add(message);
                return Task.CompletedTask;
            });
            var reader = new Mock<IAsyncStreamReader<StreamingMessage>>();
            reader.Setup(stream => stream.MoveNext(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            var rpc = new Mock<FunctionRpcClient>();
            rpc.Setup(client => client.EventStream(null, null, It.IsAny<CancellationToken>())).Returns(() =>
            {
                StreamCalls++;
                return new AsyncDuplexStreamingCall<StreamingMessage, StreamingMessage>(writer.Object, reader.Object, Task.FromResult(new Metadata()),
                    () => Status.DefaultSuccess, () => new Metadata(), () => StreamDisposed = true);
            });
            Client = new(Output, new GrpcWorkerStartupOptions { WorkerId = "test-worker" }, Mock.Of<IMessageProcessor>(), Channel, rpc.Object);
        }

        public void Dispose()
        {
            Output.Channel.Writer.TryComplete();
            Channel.Dispose();
            Listener.Stop();
            _loggerFactory.Dispose();
        }
    }

    private sealed class ConnectionDiagnostics : ILoggerProvider, ILogger
    {
        public ConcurrentQueue<byte> Attempts { get; } = new();
        public TaskCompletionSource<bool> FirstFailure { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ILogger CreateLogger(string categoryName) => this;
        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
        public void Dispose() { }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (eventId.Name == "ConnectingSocket")
            {
                Attempts.Enqueue(0);
            }
            else if (eventId.Name == "ErrorConnectingSocket")
            {
                Assert.NotNull(exception);
                FirstFailure.TrySetResult(true);
            }
        }
    }
}
