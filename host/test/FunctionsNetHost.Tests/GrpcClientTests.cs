// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net.Http;
using System.Net.Sockets;
using FunctionsNetHost.Grpc;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Xunit;

namespace FunctionsNetHost.Tests
{
    public class GrpcClientTests
    {
        [Fact]
        public async Task ConnectAsync_RetriesTransientConnectionFailureAndEventuallySucceeds()
        {
            var startupOptions = CreateStartupOptions();
            startupOptions.InitialConnectionRetryDelay = TimeSpan.Zero;

            var appLoader = new AppLoader(startupOptions);
            var firstStream = new TestGrpcEventStream(_ => throw CreateConnectionUnavailableException());
            var secondStream = new TestGrpcEventStream(message =>
            {
                Assert.Equal(startupOptions.WorkerId, message.StartStream.WorkerId);
                return Task.CompletedTask;
            });

            var attempts = 0;
            var delays = new List<TimeSpan>();
            var grpcClient = new GrpcClient(
                startupOptions,
                appLoader,
                () =>
                {
                    attempts++;
                    return attempts == 1 ? firstStream : secondStream;
                },
                delay =>
                {
                    delays.Add(delay);
                    return Task.CompletedTask;
                },
                log: _ => { });

            using var eventStream = await grpcClient.ConnectAsync();

            Assert.Same(secondStream, eventStream);
            Assert.Equal(2, attempts);
            Assert.Single(delays);
            Assert.True(firstStream.IsDisposed);
            Assert.False(secondStream.IsDisposed);
            Assert.Equal(1, firstStream.WriteAttemptCount);
            Assert.Equal(1, secondStream.WriteAttemptCount);
        }

        [Fact]
        public async Task ConnectAsync_ThrowsAfterRetryBudgetIsExhausted()
        {
            var startupOptions = CreateStartupOptions();
            startupOptions.InitialConnectionRetryTimeout = TimeSpan.Zero;
            startupOptions.InitialConnectionRetryDelay = TimeSpan.Zero;

            var appLoader = new AppLoader(startupOptions);
            var failingStream = new TestGrpcEventStream(_ => throw CreateConnectionUnavailableException());
            var attempts = 0;
            var delays = 0;
            var grpcClient = new GrpcClient(
                startupOptions,
                appLoader,
                () =>
                {
                    attempts++;
                    return failingStream;
                },
                _ =>
                {
                    delays++;
                    return Task.CompletedTask;
                },
                log: _ => { });

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => grpcClient.ConnectAsync());

            Assert.Contains("Failed to establish initial gRPC connection", exception.Message);
            Assert.IsType<HttpRequestException>(exception.InnerException);
            Assert.Equal(1, attempts);
            Assert.Equal(0, delays);
            Assert.True(failingStream.IsDisposed);
        }

        [Fact]
        public async Task ConnectAsync_DoesNotRetryNonTransientFailures()
        {
            var startupOptions = CreateStartupOptions();
            startupOptions.InitialConnectionRetryDelay = TimeSpan.Zero;

            var appLoader = new AppLoader(startupOptions);
            var expectedException = new InvalidOperationException("Configuration error.");
            var attempts = 0;
            var delays = 0;
            var grpcClient = new GrpcClient(
                startupOptions,
                appLoader,
                () =>
                {
                    attempts++;
                    throw expectedException;
                },
                _ =>
                {
                    delays++;
                    return Task.CompletedTask;
                },
                log: _ => { });

            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(() => grpcClient.ConnectAsync());

            Assert.Same(expectedException, actualException);
            Assert.Equal(1, attempts);
            Assert.Equal(0, delays);
        }

        [Fact]
        public async Task ConnectAsync_CoalescesRepeatedIdenticalFailuresAndLogsSuccessSummary()
        {
            var startupOptions = CreateStartupOptions();
            startupOptions.InitialConnectionRetryDelay = TimeSpan.FromMilliseconds(100);

            var appLoader = new AppLoader(startupOptions);
            var successfulStream = new TestGrpcEventStream(_ => Task.CompletedTask);
            var logs = new List<string>();
            var currentTime = new DateTimeOffset(2026, 4, 24, 0, 0, 0, TimeSpan.Zero);
            var attempts = 0;
            var grpcClient = new GrpcClient(
                startupOptions,
                appLoader,
                () =>
                {
                    attempts++;

                    return attempts <= 101
                        ? new TestGrpcEventStream(_ => throw CreateConnectionUnavailableException())
                        : successfulStream;
                },
                delay =>
                {
                    currentTime = currentTime.Add(delay);
                    return Task.CompletedTask;
                },
                logs.Add,
                () => currentTime);

            using var eventStream = await grpcClient.ConnectAsync();

            Assert.Same(successfulStream, eventStream);
            Assert.Contains(logs, message =>
                message.Contains("is still retrying", StringComparison.Ordinal)
                && message.Contains("Retried 101 times over 00:00:10", StringComparison.Ordinal)
                && message.Contains("HttpRequestException: The host endpoint is not listening yet.", StringComparison.Ordinal));
            Assert.Contains(logs, message =>
                message.Contains("Initial gRPC connection established", StringComparison.Ordinal)
                && message.Contains("AttemptCount:102", StringComparison.Ordinal)
                && message.Contains("RetryCount:101", StringComparison.Ordinal));
        }

        [Fact]
        public async Task ConnectAsync_LogsRepeatedFailureSummaryWhenFailureSignatureChanges()
        {
            var startupOptions = CreateStartupOptions();
            startupOptions.InitialConnectionRetryDelay = TimeSpan.FromMilliseconds(100);

            var appLoader = new AppLoader(startupOptions);
            var successfulStream = new TestGrpcEventStream(_ => Task.CompletedTask);
            var logs = new List<string>();
            var currentTime = new DateTimeOffset(2026, 4, 24, 0, 0, 0, TimeSpan.Zero);
            var attempts = 0;
            var grpcClient = new GrpcClient(
                startupOptions,
                appLoader,
                () =>
                {
                    attempts++;

                    return attempts switch
                    {
                        1 or 2 => new TestGrpcEventStream(_ => throw CreateConnectionUnavailableException("The host endpoint is not listening yet.")),
                        3 => new TestGrpcEventStream(_ => throw CreateConnectionUnavailableException("The host endpoint is still booting.")),
                        _ => successfulStream
                    };
                },
                delay =>
                {
                    currentTime = currentTime.Add(delay);
                    return Task.CompletedTask;
                },
                logs.Add,
                () => currentTime);

            using var eventStream = await grpcClient.ConnectAsync();

            Assert.Same(successfulStream, eventStream);
            Assert.Contains(logs, message =>
                message.Contains("Retried 2 times over 00:00:00.2000000", StringComparison.Ordinal)
                && message.Contains("HttpRequestException: The host endpoint is not listening yet.", StringComparison.Ordinal));
            Assert.Contains(logs, message =>
                message.Contains("Initial gRPC connection established", StringComparison.Ordinal)
                && message.Contains("AttemptCount:4", StringComparison.Ordinal)
                && message.Contains("RetryCount:3", StringComparison.Ordinal));
        }

        [Fact]
        public async Task ConnectAsync_LogsConnectionSuccessWhenNoRetriesAreNeeded()
        {
            var startupOptions = CreateStartupOptions();
            var appLoader = new AppLoader(startupOptions);
            var successfulStream = new TestGrpcEventStream(_ => Task.CompletedTask);
            var logs = new List<string>();
            var grpcClient = new GrpcClient(
                startupOptions,
                appLoader,
                () => successfulStream,
                log: logs.Add);

            using var eventStream = await grpcClient.ConnectAsync();

            Assert.Same(successfulStream, eventStream);
            Assert.Contains(logs, message =>
                message.Contains("Initial gRPC connection established", StringComparison.Ordinal)
                && message.Contains("AttemptCount:1", StringComparison.Ordinal)
                && message.Contains("RetryCount:0", StringComparison.Ordinal));
            Assert.Contains(logs, message => message.Contains("StartStream sent", StringComparison.Ordinal));
            Assert.DoesNotContain(logs, message => message.Contains("is still retrying", StringComparison.Ordinal));
        }

        private static GrpcWorkerStartupOptions CreateStartupOptions()
        {
            return new GrpcWorkerStartupOptions
            {
                ServerUri = new Uri("http://127.0.0.1:5000"),
                WorkerId = "worker-id",
                CommandLineArgs = Array.Empty<string>()
            };
        }

        private static HttpRequestException CreateConnectionUnavailableException(string message = "The host endpoint is not listening yet.")
        {
            return new HttpRequestException(
                message,
                new SocketException((int)SocketError.ConnectionRefused));
        }

        private sealed class TestGrpcEventStream(Func<StreamingMessage, Task> onWrite) : IGrpcEventStream
        {
            private readonly TestClientStreamWriter _requestStream = new(onWrite);

            public bool IsDisposed { get; private set; }

            public int WriteAttemptCount => _requestStream.WriteAttemptCount;

            public IAsyncStreamReader<StreamingMessage> ResponseStream { get; } = new TestAsyncStreamReader();

            public IClientStreamWriter<StreamingMessage> RequestStream => _requestStream;

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        private sealed class TestClientStreamWriter(Func<StreamingMessage, Task> onWrite) : IClientStreamWriter<StreamingMessage>
        {
            public int WriteAttemptCount { get; private set; }

            public WriteOptions? WriteOptions { get; set; }

            public Task CompleteAsync() => Task.CompletedTask;

            public Task WriteAsync(StreamingMessage message)
            {
                WriteAttemptCount++;
                return onWrite(message);
            }
        }

        private sealed class TestAsyncStreamReader : IAsyncStreamReader<StreamingMessage>
        {
            public StreamingMessage Current { get; private set; } = new();

            public Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                Current = new StreamingMessage();
                return Task.FromResult(false);
            }
        }
    }
}
