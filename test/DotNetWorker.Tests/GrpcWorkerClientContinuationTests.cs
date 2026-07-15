// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests;

public class GrpcWorkerClientContinuationTests
{
    [Fact]
    public void StartWriterAsyncContinuationDoesNotRequireOriginalSynchronizationContext()
    {
        SynchronizationContext originalContext = SynchronizationContext.Current;
        using var context = new DroppingSingleThreadSynchronizationContext();

        try
        {
            SynchronizationContext.SetSynchronizationContext(context);

            Channel<StreamingMessage> channel = Channel.CreateUnbounded<StreamingMessage>();
            var outputChannel = new GrpcHostChannel(channel);
            object client = CreateGrpcWorkerClient(outputChannel);
            var requestStream = new FakeClientStreamWriter();

            Task writerTask = StartWriterAsync(client, requestStream);

            var firstMessage = new StreamingMessage { RequestId = "1" };
            var secondMessage = new StreamingMessage { RequestId = "2" };

            Assert.True(channel.Writer.TryWrite(firstMessage));
            TaskCompletionSource<bool> firstWrite = WaitFor(requestStream.FirstWriteStarted.Task, "The first write did not start.");

            context.Shutdown();
            firstWrite.SetResult(true);

            Assert.True(channel.Writer.TryWrite(secondMessage));
            channel.Writer.Complete();

            TaskCompletionSource<bool> secondWrite = WaitFor(requestStream.SecondWriteStarted.Task, "The second write did not start after the context was shut down.");
            secondWrite.SetResult(true);

            WaitFor(writerTask, "The production write loop did not drain both messages.");

            Assert.Collection(requestStream.WrittenMessages,
                message => Assert.Same(firstMessage, message),
                message => Assert.Same(secondMessage, message));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    private static object CreateGrpcWorkerClient(GrpcHostChannel outputChannel)
    {
        Type clientType = typeof(GrpcWorkerClientFactory).GetNestedType("GrpcWorkerClient", BindingFlags.NonPublic);
        Assert.NotNull(clientType);

        var startupOptions = new GrpcWorkerStartupOptions
        {
            HostEndpoint = new Uri("http://localhost:5000"),
            WorkerId = "test-worker",
            GrpcMaxMessageLength = 1024
        };

        object client = Activator.CreateInstance(
            clientType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { outputChannel, startupOptions, new NoOpMessageProcessor() },
            culture: null);

        Assert.NotNull(client);
        return client;
    }

    private static Task StartWriterAsync(object client, IClientStreamWriter<StreamingMessage> requestStream)
    {
        MethodInfo method = client.GetType().GetMethod("StartWriterAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        // This drives the real StartWriterAsync loop; reverting its ConfigureAwait(false) calls makes this test hang/fail.
        return (Task)method.Invoke(client, new object[] { requestStream });
    }

    private static T WaitFor<T>(Task<T> task, string message)
    {
        Assert.True(task.Wait(TimeSpan.FromSeconds(2)), message);
        return task.GetAwaiter().GetResult();
    }

    private static void WaitFor(Task task, string message)
    {
        Assert.True(task.Wait(TimeSpan.FromSeconds(2)), message);
        task.GetAwaiter().GetResult();
    }

    private sealed class FakeClientStreamWriter : IClientStreamWriter<StreamingMessage>
    {
        private readonly List<StreamingMessage> _writtenMessages = new();
        private readonly object _syncLock = new();
        private int _writeCount;

        public TaskCompletionSource<TaskCompletionSource<bool>> FirstWriteStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<TaskCompletionSource<bool>> SecondWriteStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public WriteOptions WriteOptions { get; set; }

        public IReadOnlyList<StreamingMessage> WrittenMessages
        {
            get
            {
                lock (_syncLock)
                {
                    return _writtenMessages.ToArray();
                }
            }
        }

        public Task CompleteAsync() => Task.CompletedTask;

        public Task WriteAsync(StreamingMessage message)
        {
            var write = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int writeCount;

            lock (_syncLock)
            {
                _writtenMessages.Add(message);
                writeCount = ++_writeCount;
            }

            if (writeCount == 1)
            {
                FirstWriteStarted.SetResult(write);
            }
            else if (writeCount == 2)
            {
                SecondWriteStarted.SetResult(write);
            }

            return write.Task;
        }
    }

    private sealed class NoOpMessageProcessor : IMessageProcessor
    {
        public Task ProcessMessageAsync(StreamingMessage request) => Task.CompletedTask;
    }

    private sealed class DroppingSingleThreadSynchronizationContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object State)> _workItems = new();
        private readonly Thread _thread;
        private int _shutdown;

        public DroppingSingleThreadSynchronizationContext()
        {
            _thread = new Thread(RunOnDedicatedThread)
            {
                IsBackground = true,
                Name = nameof(DroppingSingleThreadSynchronizationContext)
            };

            _thread.Start();
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (Volatile.Read(ref _shutdown) == 1)
            {
                return;
            }

            try
            {
                _workItems.Add((d, state));
            }
            catch (InvalidOperationException)
            {
            }
        }

        public void Shutdown()
        {
            if (Interlocked.Exchange(ref _shutdown, 1) == 0)
            {
                _workItems.CompleteAdding();
            }
        }

        public void Dispose()
        {
            Shutdown();
            _thread.Join(TimeSpan.FromSeconds(2));
            _workItems.Dispose();
        }

        private void RunOnDedicatedThread()
        {
            SetSynchronizationContext(this);

            foreach ((SendOrPostCallback callback, object state) in _workItems.GetConsumingEnumerable())
            {
                callback(state);
            }
        }
    }
}