// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Invocation;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class TaskMethodInvokerTests
    {
        [Fact]
        public async Task InvokeAsync_DelegatesToLambda()
        {
            // Arrange
            object expectedInstance = new object();
            object[] expectedArguments = new object[0];
            bool invoked = false;
            object instance = null;
            object[] arguments = null;
            Func<object, object[], Task<object>> lambda = (i, a) =>
            {
                invoked = true;
                instance = i;
                arguments = a;
                return Task.FromResult<object>(null);
            };

            IMethodInvoker<object, object> invoker = CreateProductUnderTest(lambda);

            // Act
            await invoker.InvokeAsync(expectedInstance, expectedArguments);

            // Assert
            Assert.True(invoked);
            Assert.Same(expectedInstance, instance);
            Assert.Same(expectedArguments, arguments);
        }

        [Fact]
        public async Task InvokeAsync_IfLambdaThrows_PropogatesException()
        {
            // Arrange
            InvalidOperationException expectedException = new InvalidOperationException();
            Func<object, object[], Task<object>> lambda = (i1, i2) =>
            {
                throw expectedException;
            };

            IMethodInvoker<object, object> invoker = CreateProductUnderTest(lambda);
            object instance = null;
            object[] arguments = null;

            // Act & Assert
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => invoker.InvokeAsync(instance, arguments));
            Assert.Same(expectedException, exception);
        }

        [Fact]
        public void InvokeAsync_IfLambdaReturnsCanceledTask_ReturnsCanceledTask()
        {
            // Arrange
            Func<object, object[], Task<object>> lambda = (i1, i2) =>
            {
                TaskCompletionSource<object> source = new TaskCompletionSource<object>();
                source.SetCanceled();
                return source.Task;
            };

            IMethodInvoker<object, object> invoker = CreateProductUnderTest(lambda);
            object instance = null;
            object[] arguments = null;

            // Act
            Task task = invoker.InvokeAsync(instance, arguments);

            // Assert
            Assert.NotNull(task);
            task.WaitUntilCompleted();
            Assert.Equal(TaskStatus.Canceled, task.Status);
        }

        [Fact]
        public void InvokeAsync_IfLambdaReturnsFaultedTask_ReturnsFaultedTask()
        {
            // Arrange
            Exception expectedException = new InvalidOperationException();
            Func<object, object[], Task<object>> lambda = (i1, i2) =>
            {
                TaskCompletionSource<object> source = new TaskCompletionSource<object>();
                source.SetException(expectedException);
                return source.Task;
            };

            IMethodInvoker<object, object> invoker = CreateProductUnderTest(lambda);
            object instance = null;
            object[] arguments = null;

            // Act
            Task task = invoker.InvokeAsync(instance, arguments);

            // Assert
            Assert.NotNull(task);
            task.WaitUntilCompleted();
            Assert.Equal(TaskStatus.Faulted, task.Status);
            Assert.NotNull(task.Exception);
            Assert.Same(expectedException, task.Exception.InnerException);
        }

        [Fact]
        public void InvokeAsync_IfLambdaReturnsNull_ReturnsNull()
        {
            // Arrange
            Func<object, object[], Task<object>> lambda = (i1, i2) => null;
            IMethodInvoker<object, object> invoker = CreateProductUnderTest(lambda);
            object instance = null;
            object[] arguments = null;

            // Act
            Task task = invoker.InvokeAsync(instance, arguments);

            // Assert
            Assert.Null(task);
        }

        [Fact]
        public void InvokeAsync_IfLambdaReturnsTaskDelayTask_ReturnsCompletedTask()
        {
            // Arrange
            Func<object, object[], Task<object>> lambda = async (i1, i2) =>
            {
                Task innerTask = Task.Delay(1);
                Assert.False(innerTask.GetType().IsGenericType); // Guard
                await innerTask;
                return null;
            };

            IMethodInvoker<object, object> invoker = CreateProductUnderTest(lambda);
            object instance = null;
            object[] arguments = null;

            // Act
            Task task = invoker.InvokeAsync(instance, arguments);

            // Assert
            Assert.NotNull(task);
            task.WaitUntilCompleted();
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        }

        [Fact]
        public void InvokeAsync_IfLambdaReturnsTaskWhenAllTask_ReturnsCompletedTask()
        {
            // Arrange
            Func<object, object[], Task<object>> lambda = async (i1, i2) =>
            {
                Task innerTask = Task.WhenAll(Task.Delay(1));
                Assert.False(innerTask.GetType().IsGenericType); // Guard
                await innerTask;
                return null;
            };

            IMethodInvoker<object, object> invoker = CreateProductUnderTest(lambda);
            object instance = null;
            object[] arguments = null;

            // Act
            Task task = invoker.InvokeAsync(instance, arguments);

            // Assert
            Assert.NotNull(task);
            task.WaitUntilCompleted();
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        }

        [Fact]
        public void InvokeAsync_IfLambdaReturnsTaskWhenAllCancelledTask_ReturnsCancelledTask()
        {
            // Arrange
            Func<object, object[], Task<object>> lambda = async (i1, i2) =>
            {
                var cancellationSource = new System.Threading.CancellationTokenSource();
                Task innerTask = new Task(() => { }, cancellationSource.Token);
                Assert.False(innerTask.GetType().IsGenericType); // Guard
                cancellationSource.Cancel();
                await Task.WhenAll(innerTask);
                return null;
            };

            IMethodInvoker<object, object> invoker = CreateProductUnderTest(lambda);
            object instance = null;
            object[] arguments = null;

            // Act
            Task task = invoker.InvokeAsync(instance, arguments);

            // Assert
            Assert.NotNull(task);
            task.WaitUntilCompleted();
            Assert.Equal(TaskStatus.Canceled, task.Status);
        }

        [Fact]
        public void InvokeAsync_IfLambdaReturnsTaskWhenAllFaultedTask_ReturnsFaultedTask()
        {
            // Arrange
            Exception expectedException = new InvalidOperationException();
            Func<object, object[], Task<object>> lambda = async (i1, i2) =>
            {
                Task innerTask = new Task(() => { throw expectedException; });
                innerTask.Start();
                Assert.False(innerTask.GetType().IsGenericType); // Guard
                await Task.WhenAll(innerTask);
                return null;
            };

            IMethodInvoker<object, object> invoker = CreateProductUnderTest(lambda);
            object instance = null;
            object[] arguments = null;

            // Act
            Task task = invoker.InvokeAsync(instance, arguments);

            // Assert
            Assert.NotNull(task);
            task.WaitUntilCompleted();
            Assert.Equal(TaskStatus.Faulted, task.Status);
            Assert.NotNull(task.Exception);
            Assert.Same(expectedException, task.Exception.InnerException);
        }

        [Fact]
        public void InvokeAsync_IfLambdaReturnsTaskWhenAllTaskWithReturnTypes_ReturnsCompletedTask()
        {
            // Arrange
            Func<object, object[], Task<object>> lambda = async (i1, i2) =>
            {
                Task innerTask = Task.WhenAll(Task.FromResult<object>(null));
                await innerTask;
                return null;
            };

            IMethodInvoker<object, object> invoker = CreateProductUnderTest(lambda);
            object instance = null;
            object[] arguments = null;

            // Act
            Task task = invoker.InvokeAsync(instance, arguments);

            // Assert
            Assert.NotNull(task);
            task.WaitUntilCompleted();
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        }

        [Fact]
        public void InvokeAsync_IfLambdaReturnsTaskWhenAllCancelledTaskWithReturnTypes_ReturnsCancelledTask()
        {
            // Arrange
            Func<object, object[], Task<object>> lambda = async (i1, i2) =>
            {
                TaskCompletionSource<object> source = new TaskCompletionSource<object>();
                source.SetCanceled();
                await Task.WhenAll(source.Task);
                return null;
            };

            IMethodInvoker<object, object> invoker = CreateProductUnderTest(lambda);
            object instance = null;
            object[] arguments = null;

            // Act
            Task task = invoker.InvokeAsync(instance, arguments);

            // Assert
            Assert.NotNull(task);
            task.WaitUntilCompleted();
            Assert.Equal(TaskStatus.Canceled, task.Status);
        }

        [Fact]
        public void InvokeAsync_IfLambdaReturnsTaskWhenAllFaultedTaskWithReturnTypes_ReturnsFaultedTask()
        {
            // Arrange
            Exception expectedException = new InvalidOperationException();
            Func<object, object[], Task<object>> lambda = async (i1, i2) =>
            {
                TaskCompletionSource<object> source = new TaskCompletionSource<object>();
                source.SetException(expectedException);
                await Task.WhenAll(source.Task);
                return null;
            };

            IMethodInvoker<object, object> invoker = CreateProductUnderTest(lambda);
            object instance = null;
            object[] arguments = null;

            // Act
            Task task = invoker.InvokeAsync(instance, arguments);

            // Assert
            Assert.NotNull(task);
            task.WaitUntilCompleted();
            Assert.Equal(TaskStatus.Faulted, task.Status);
            Assert.NotNull(task.Exception);
            Assert.Same(expectedException, task.Exception.InnerException);
        }

        private static TaskMethodInvoker<object, object> CreateProductUnderTest(Func<object, object[], Task<object>> lambda)
        {
            return new TaskMethodInvoker<object, object>(lambda);
        }
    }
}
