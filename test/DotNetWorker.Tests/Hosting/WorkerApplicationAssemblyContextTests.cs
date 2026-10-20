// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Hosting;

public class WorkerApplicationAssemblyContextTests
{
    [Fact]
    public void ResolveOrEntryAssembly_WithoutScope_ReturnsEntryAssembly()
    {
        Assert.Same(Assembly.GetEntryAssembly(), WorkerApplicationAssemblyContext.ResolveOrEntryAssembly());
    }

    [Fact]
    public void Push_NullAssembly_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => WorkerApplicationAssemblyContext.Push(null));
    }

    [Fact]
    public void Push_NestedSameAssembly_RestoresPreviousScope()
    {
        Assembly assembly = typeof(WorkerApplicationAssemblyContextTests).Assembly;

        using (WorkerApplicationAssemblyContext.Push(assembly))
        {
            Assert.Same(assembly, WorkerApplicationAssemblyContext.ResolveOrEntryAssembly());

            using (WorkerApplicationAssemblyContext.Push(assembly))
            {
                Assert.Same(assembly, WorkerApplicationAssemblyContext.ResolveOrEntryAssembly());
            }

            Assert.Same(assembly, WorkerApplicationAssemblyContext.ResolveOrEntryAssembly());
        }

        Assert.Same(Assembly.GetEntryAssembly(), WorkerApplicationAssemblyContext.ResolveOrEntryAssembly());
    }

    [Fact]
    public void Push_DifferentNestedAssembly_ThrowsWithoutChangingScope()
    {
        Assembly assembly = typeof(WorkerApplicationAssemblyContextTests).Assembly;
        Assembly differentAssembly = typeof(string).Assembly;

        using (WorkerApplicationAssemblyContext.Push(assembly))
        {
            Assert.Throws<InvalidOperationException>(() => WorkerApplicationAssemblyContext.Push(differentAssembly));
            Assert.Same(assembly, WorkerApplicationAssemblyContext.ResolveOrEntryAssembly());
        }
    }

    [Fact]
    public void Dispose_OutOfOrder_ThrowsAndAllowsReverseOrderCleanup()
    {
        Assembly assembly = typeof(WorkerApplicationAssemblyContextTests).Assembly;
        IDisposable outer = WorkerApplicationAssemblyContext.Push(assembly);
        IDisposable inner = WorkerApplicationAssemblyContext.Push(assembly);

        Assert.Throws<InvalidOperationException>(() => outer.Dispose());

        inner.Dispose();
        outer.Dispose();
        Assert.Same(Assembly.GetEntryAssembly(), WorkerApplicationAssemblyContext.ResolveOrEntryAssembly());
    }

    [Fact]
    public async Task Push_ParallelExecutionContexts_RemainIsolated()
    {
        Assembly firstAssembly = typeof(WorkerApplicationAssemblyContextTests).Assembly;
        Assembly secondAssembly = typeof(string).Assembly;
        var firstReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task first = Task.Run(async () =>
        {
            using (WorkerApplicationAssemblyContext.Push(firstAssembly))
            {
                firstReady.SetResult();
                await secondReady.Task;
                Assert.Same(firstAssembly, WorkerApplicationAssemblyContext.ResolveOrEntryAssembly());
            }
        });

        Task second = Task.Run(async () =>
        {
            using (WorkerApplicationAssemblyContext.Push(secondAssembly))
            {
                secondReady.SetResult();
                await firstReady.Task;
                Assert.Same(secondAssembly, WorkerApplicationAssemblyContext.ResolveOrEntryAssembly());
            }
        });

        await Task.WhenAll(first, second);
        Assert.Same(Assembly.GetEntryAssembly(), WorkerApplicationAssemblyContext.ResolveOrEntryAssembly());
    }

    [Fact]
    public async Task Push_WhenExecutionContextFlowIsSuppressed_DoesNotLeakScope()
    {
        Assembly assembly = typeof(WorkerApplicationAssemblyContextTests).Assembly;
        Task<Assembly> task;

        using (WorkerApplicationAssemblyContext.Push(assembly))
        {
            using (ExecutionContext.SuppressFlow())
            {
                task = Task.Run(WorkerApplicationAssemblyContext.ResolveOrEntryAssembly);
            }

            Assert.Same(Assembly.GetEntryAssembly(), await task);
            Assert.Same(assembly, WorkerApplicationAssemblyContext.ResolveOrEntryAssembly());
        }
    }
}
