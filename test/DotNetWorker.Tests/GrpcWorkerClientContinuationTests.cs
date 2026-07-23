// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests;

public class GrpcWorkerClientContinuationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task StartWriterAsyncIncompleteWriteContinuationDoesNotRequireOriginalSynchronizationContext()
    {
        using ServiceProvider provider = CreateServiceProvider();
        GrpcHostChannel outputChannel = provider.GetRequiredService<GrpcHostChannel>();
        object client = CreateGrpcWorkerClient(outputChannel);
        var requestStream = new PendingWriteClientStreamWriter();
        using var context = new DroppingSingleThreadSynchronizationContext();

        Task writerTask = StartWriterAsyncWithNoSynchronizationContext(client, requestStream);

        var firstMessage = new StreamingMessage { RequestId = "1" };

        await context.Run(() => Assert.True(outputChannel.Channel.Writer.TryWrite(firstMessage))).WaitAsync(Timeout);

        TaskCompletionSource<bool> firstWrite = await WaitForAsync(requestStream.FirstWritePending.Task, "The first write did not start.");
        WriteRecord firstWriteRecord = requestStream.GetWrite(0);
        Assert.Same(firstMessage, firstWriteRecord.Message);
        Assert.Same(context, firstWriteRecord.SynchronizationContext);

        context.Shutdown();
        firstWrite.SetResult(true);
        outputChannel.Channel.Writer.Complete();

        await WaitForAsync(writerTask, "The production write loop did not complete after the pending write completed.");
    }

    [Fact]
    public async Task StartWriterAsyncSecondReadContinuationDoesNotRequireOriginalSynchronizationContext()
    {
        using ServiceProvider provider = CreateServiceProvider();
        GrpcHostChannel outputChannel = provider.GetRequiredService<GrpcHostChannel>();
        object client = CreateGrpcWorkerClient(outputChannel);
        var requestStream = new CompletedWriteClientStreamWriter();
        using var context = new DroppingSingleThreadSynchronizationContext();

        Task writerTask = StartWriterAsyncWithNoSynchronizationContext(client, requestStream);

        var firstMessage = new StreamingMessage { RequestId = "1" };
        var secondMessage = new StreamingMessage { RequestId = "2" };

        await context.Run(() => Assert.True(outputChannel.Channel.Writer.TryWrite(firstMessage))).WaitAsync(Timeout);

        WriteRecord firstWriteRecord = await WaitForAsync(requestStream.FirstWriteStarted.Task, "The first write did not start.");
        Assert.Same(firstMessage, firstWriteRecord.Message);
        Assert.Same(context, firstWriteRecord.SynchronizationContext);

        context.Shutdown();
        WriteMessageWithNoSynchronizationContext(outputChannel, secondMessage);

        WriteRecord secondWriteRecord = await WaitForAsync(requestStream.SecondWriteStarted.Task, "The second write did not start after the context was shut down.");
        Assert.Same(secondMessage, secondWriteRecord.Message);
        Assert.Null(secondWriteRecord.SynchronizationContext);

        await WaitForAsync(writerTask, "The production write loop did not complete after the channel was completed.");
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.RegisterOutputChannel();
        return services.BuildServiceProvider();
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

    private static Task StartWriterAsyncWithNoSynchronizationContext(object client, IClientStreamWriter<StreamingMessage> requestStream)
    {
        SynchronizationContext originalContext = SynchronizationContext.Current;

        try
        {
            SynchronizationContext.SetSynchronizationContext(null);
            Task writerTask = StartWriterAsync(client, requestStream);
            Assert.False(writerTask.IsCompleted, "The production write loop should park at the first empty channel read.");
            return writerTask;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    private static Task StartWriterAsync(object client, IClientStreamWriter<StreamingMessage> requestStream)
    {
        MethodInfo method = client.GetType().GetMethod("StartWriterAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (Task)method.Invoke(client, new object[] { requestStream });
    }

    private static void WriteMessageWithNoSynchronizationContext(GrpcHostChannel outputChannel, StreamingMessage message)
    {
        SynchronizationContext originalContext = SynchronizationContext.Current;

        try
        {
            SynchronizationContext.SetSynchronizationContext(null);
            Assert.True(outputChannel.Channel.Writer.TryWrite(message));
            outputChannel.Channel.Writer.Complete();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    private static async Task<T> WaitForAsync<T>(Task<T> task, string message)
    {
        Task completedTask = await Task.WhenAny(task, Task.Delay(Timeout));
        Assert.True(ReferenceEquals(task, completedTask), message);
        return await task;
    }

    private static async Task WaitForAsync(Task task, string message)
    {
        Task completedTask = await Task.WhenAny(task, Task.Delay(Timeout));
        Assert.True(ReferenceEquals(task, completedTask), message);
        await task;
    }

    private abstract class RecordingClientStreamWriter : IClientStreamWriter<StreamingMessage>
    {
        private readonly List<WriteRecord> _writes = new();
        private readonly object _syncLock = new();

        public TaskCompletionSource<WriteRecord> FirstWriteStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<WriteRecord> SecondWriteStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public WriteOptions WriteOptions { get; set; }

        public Task CompleteAsync() => Task.CompletedTask;

        public Task WriteAsync(StreamingMessage message)
        {
            WriteRecord record = RecordWrite(message);
            return WriteCoreAsync(record);
        }

        public WriteRecord GetWrite(int index)
        {
            lock (_syncLock)
            {
                return _writes[index];
            }
        }

        protected abstract Task WriteCoreAsync(WriteRecord record);

        private WriteRecord RecordWrite(StreamingMessage message)
        {
            var record = new WriteRecord(message, SynchronizationContext.Current);
            int writeCount;

            lock (_syncLock)
            {
                _writes.Add(record);
                writeCount = _writes.Count;
            }

            if (writeCount == 1)
            {
                FirstWriteStarted.SetResult(record);
            }
            else if (writeCount == 2)
            {
                SecondWriteStarted.SetResult(record);
            }

            return record;
        }
    }

    private sealed class PendingWriteClientStreamWriter : RecordingClientStreamWriter
    {
        private readonly TaskCompletionSource<bool> _firstWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<TaskCompletionSource<bool>> FirstWritePending { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override Task WriteCoreAsync(WriteRecord record)
        {
            FirstWritePending.SetResult(_firstWrite);
            return _firstWrite.Task;
        }
    }

    private sealed class CompletedWriteClientStreamWriter : RecordingClientStreamWriter
    {
        protected override Task WriteCoreAsync(WriteRecord record) => Task.CompletedTask;
    }

    private sealed class WriteRecord
    {
        public WriteRecord(StreamingMessage message, SynchronizationContext synchronizationContext)
        {
            Message = message;
            SynchronizationContext = synchronizationContext;
        }

        public StreamingMessage Message { get; }

        public SynchronizationContext SynchronizationContext { get; }
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

        public Task Run(Action action)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            Post(_ =>
            {
                try
                {
                    action();
                    completion.SetResult(true);
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                }
            }, null);

            return completion.Task;
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