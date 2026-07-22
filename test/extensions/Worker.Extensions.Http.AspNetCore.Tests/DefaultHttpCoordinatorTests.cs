// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Worker.Extensions.Http.AspNetCore.Tests
{
    public class DefaultHttpCoordinatorTests
    {
        private readonly DefaultHttpCoordinator _coordinator;
        private readonly ExtensionTrace _logger;

        public DefaultHttpCoordinatorTests()
        {
            var loggerFactory = new NullLoggerFactory();
            _logger = new ExtensionTrace(loggerFactory);
            _coordinator = new DefaultHttpCoordinator(_logger);
        }

        [Fact]
        public async Task SetHttpContextAsync_WhenFunctionContextSetFirst_ReturnsSuccessfully()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var httpContext = new DefaultHttpContext();
            var functionContext = CreateTestFunctionContext();

            // Act
            var setFunctionTask = _coordinator.SetFunctionContextAsync(invocationId, functionContext);
            var setHttpTask = _coordinator.SetHttpContextAsync(invocationId, httpContext);
            _ = _coordinator.RunFunctionInvocationAsync(invocationId);

            var resultFunctionContext = await setHttpTask;
            var resultHttpContext = await setFunctionTask;

            // Assert
            Assert.Same(functionContext, resultFunctionContext);
            Assert.Same(httpContext, resultHttpContext);
        }

        [Fact]
        public async Task SetHttpContextAsync_WhenRequestCancelled_ThrowsOperationCanceledException()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var cts = new CancellationTokenSource();
            var httpContext = new DefaultHttpContext
            {
                RequestAborted = cts.Token
            };

            // Act
            var setHttpTask = _coordinator.SetHttpContextAsync(invocationId, httpContext);
            cts.Cancel();

            // Assert
            var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () => await setHttpTask);
            Assert.Contains($"HTTP request was cancelled while waiting for the function context to be set. Invocation: '{invocationId}'", exception.Message);
        }

        [Fact]
        public async Task SetHttpContextAsync_WhenFunctionContextTimesOut_ThrowsTimeoutException()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var httpContext = new DefaultHttpContext();

            // Act - Don't set the function context, causing timeout
            var setHttpTask = _coordinator.SetHttpContextAsync(invocationId, httpContext);

            // Assert
            var exception = await Assert.ThrowsAsync<TimeoutException>(async () => await setHttpTask);
            Assert.Contains($"Timed out waiting for the function context to be set. Invocation: '{invocationId}'", exception.Message);
        }

        [Fact]
        public async Task SetFunctionContextAsync_WhenHttpContextSetFirst_ReturnsSuccessfully()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var httpContext = new DefaultHttpContext();
            var functionContext = CreateTestFunctionContext();

            // Act
            var setHttpTask = _coordinator.SetHttpContextAsync(invocationId, httpContext);
            var setFunctionTask = _coordinator.SetFunctionContextAsync(invocationId, functionContext);
            _ = _coordinator.RunFunctionInvocationAsync(invocationId);

            var resultFunctionContext = await setHttpTask;
            var resultHttpContext = await setFunctionTask;

            // Assert
            Assert.Same(functionContext, resultFunctionContext);
            Assert.Same(httpContext, resultHttpContext);
        }

        [Fact]
        public async Task SetFunctionContextAsync_WhenInvocationCancelled_ThrowsOperationCanceledException()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var cts = new CancellationTokenSource();
            var functionContext = CreateTestFunctionContext(cts.Token);

            // Act
            var setFunctionTask = _coordinator.SetFunctionContextAsync(invocationId, functionContext);
            cts.Cancel();

            // Assert
            var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () => await setFunctionTask);
            Assert.Contains($"Function invocation cancelled while waiting for start call. Invocation: '{invocationId}'", exception.Message);
        }

        [Fact]
        public async Task SetFunctionContextAsync_WhenFunctionStartTimesOut_ThrowsTimeoutException()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var functionContext = CreateTestFunctionContext();

            // Act - Don't call RunFunctionInvocationAsync, causing timeout
            var setFunctionTask = _coordinator.SetFunctionContextAsync(invocationId, functionContext);

            // Assert
            var exception = await Assert.ThrowsAsync<TimeoutException>(async () => await setFunctionTask);
            Assert.Contains($"Timed out waiting for the function start call. Invocation: '{invocationId}'", exception.Message);
        }

        [Fact]
        public async Task SetFunctionContextAsync_WhenHttpContextTimesOut_ThrowsTimeoutException()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var functionContext = CreateTestFunctionContext();

            // Act
            var setFunctionTask = _coordinator.SetFunctionContextAsync(invocationId, functionContext);
            _ = _coordinator.RunFunctionInvocationAsync(invocationId);
            // Don't set HTTP context, causing timeout

            // Assert
            var exception = await Assert.ThrowsAsync<TimeoutException>(async () => await setFunctionTask);
            Assert.Contains($"Timed out waiting for the HTTP context to be set. Invocation: '{invocationId}'", exception.Message);
        }

        [Fact]
        public async Task SetFunctionContextAsync_WhenHttpContextCancelled_ThrowsOperationCanceledException()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var cts = new CancellationTokenSource();
            var functionContext = CreateTestFunctionContext(cts.Token);

            // Act
            var setFunctionTask = _coordinator.SetFunctionContextAsync(invocationId, functionContext);
            _ = _coordinator.RunFunctionInvocationAsync(invocationId);
            cts.Cancel();

            // Assert
            var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () => await setFunctionTask);
            Assert.Contains($"Function invocation cancelled while waiting for HTTP context. Invocation: '{invocationId}'", exception.Message);
        }

        [Fact]
        public async Task RunFunctionInvocationAsync_WhenContextExists_CompletesSuccessfully()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var httpContext = new DefaultHttpContext();
            var functionContext = CreateTestFunctionContext();

            var setHttpTask = _coordinator.SetHttpContextAsync(invocationId, httpContext);
            var setFunctionTask = _coordinator.SetFunctionContextAsync(invocationId, functionContext);

            // Act
            var invocationTask = _coordinator.RunFunctionInvocationAsync(invocationId);

            // Assert - Tasks should complete after invocation is run
            await setHttpTask;
            await setFunctionTask;

            _coordinator.CompleteFunctionInvocation(invocationId);
            await invocationTask;
        }

        [Fact]
        public async Task RunFunctionInvocationAsync_WhenContextDoesNotExist_ThrowsInvalidOperationException()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _coordinator.RunFunctionInvocationAsync(invocationId));
            Assert.Contains($"Context for invocation id '{invocationId}' does not exist", exception.Message);
        }

        [Fact]
        public async Task CompleteFunctionInvocation_WhenContextExists_RemovesContext()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var httpContext = new DefaultHttpContext();
            var functionContext = CreateTestFunctionContext();

            var setHttpTask = _coordinator.SetHttpContextAsync(invocationId, httpContext);
            var setFunctionTask = _coordinator.SetFunctionContextAsync(invocationId, functionContext);
            var invocationTask = _coordinator.RunFunctionInvocationAsync(invocationId);

            await setHttpTask;
            await setFunctionTask;

            // Act
            _coordinator.CompleteFunctionInvocation(invocationId);
            await invocationTask;

            // Assert - Trying to run invocation again should fail since context was removed
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _coordinator.RunFunctionInvocationAsync(invocationId));
        }

        [Fact]
        public void CompleteFunctionInvocation_WhenContextDoesNotExist_DoesNotThrow()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();

            // Act & Assert - Should not throw
            _coordinator.CompleteFunctionInvocation(invocationId);
        }

        [Fact]
        public async Task SetHttpContextAsync_WhenCalledConcurrently_AndFunctionContextAlreadySet_ThrowsInvalidOperationException()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var httpContext1 = new DefaultHttpContext();
            var httpContext2 = new DefaultHttpContext();
            var functionContext = CreateTestFunctionContext();

            // Act - Set both contexts via normal flow first
            var setHttpTask1 = _coordinator.SetHttpContextAsync(invocationId, httpContext1);
            var setFunctionTask = _coordinator.SetFunctionContextAsync(invocationId, functionContext);
            _ = _coordinator.RunFunctionInvocationAsync(invocationId);

            await setHttpTask1;
            await setFunctionTask;

            // Now call SetHttpContextAsync again - TrySetResult fails (double-set), should throw
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _coordinator.SetHttpContextAsync(invocationId, httpContext2));

            _coordinator.CompleteFunctionInvocation(invocationId);

            // Assert
            Assert.Contains($"Failed to set HTTP context for invocation id '{invocationId}'", exception.Message);
        }

        [Fact]
        public async Task SetHttpContextAsync_WhenCalledConcurrently_AndFunctionContextNotSet_ThrowsInvalidOperationException()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var httpContext1 = new DefaultHttpContext();
            var httpContext2 = new DefaultHttpContext();

            // Act - Set HTTP context but don't set function context
            var firstSetHttpTask = _coordinator.SetHttpContextAsync(invocationId, httpContext1);

            // Second call - TrySetResult fails and FunctionContextValueSource is not complete
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _coordinator.SetHttpContextAsync(invocationId, httpContext2));

            // Assert
            Assert.Contains($"Failed to set HTTP context for invocation id '{invocationId}'", exception.Message);

            // Cleanup: unblock the first task to avoid unobserved timeout exceptions
            var functionContext = CreateTestFunctionContext();
            _ = _coordinator.SetFunctionContextAsync(invocationId, functionContext);
            _ = _coordinator.RunFunctionInvocationAsync(invocationId);
            await firstSetHttpTask;
            _coordinator.CompleteFunctionInvocation(invocationId);
        }

        [Fact]
        public async Task SetFunctionContextAsync_WhenCalledConcurrently_AndHttpContextAlreadySet_ThrowsInvalidOperationException()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var httpContext = new DefaultHttpContext();
            var functionContext1 = CreateTestFunctionContext();
            var functionContext2 = CreateTestFunctionContext();

            // Act - Set both contexts via normal flow first
            var setHttpTask = _coordinator.SetHttpContextAsync(invocationId, httpContext);
            var setFunctionTask = _coordinator.SetFunctionContextAsync(invocationId, functionContext1);
            _ = _coordinator.RunFunctionInvocationAsync(invocationId);

            await setHttpTask;
            await setFunctionTask;

            // Now call SetFunctionContextAsync again - TrySetResult fails (double-set), should throw
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _coordinator.SetFunctionContextAsync(invocationId, functionContext2));

            _coordinator.CompleteFunctionInvocation(invocationId);

            // Assert
            Assert.Contains($"Failed to set function context for invocation id '{invocationId}'", exception.Message);
        }

        [Fact]
        public async Task SetFunctionContextAsync_WhenCalledConcurrently_AndHttpContextNotSet_ThrowsInvalidOperationException()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var functionContext1 = CreateTestFunctionContext();
            var functionContext2 = CreateTestFunctionContext();

            // Act - Set function context but don't set HTTP context
            var firstSetFunctionTask = _coordinator.SetFunctionContextAsync(invocationId, functionContext1);

            // Second call - TrySetResult fails and HttpContextValueSource is not complete
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _coordinator.SetFunctionContextAsync(invocationId, functionContext2));

            // Assert
            Assert.Contains($"Failed to set function context for invocation id '{invocationId}'", exception.Message);

            // Cleanup: unblock the first task to avoid unobserved timeout exceptions
            var httpContext = new DefaultHttpContext();
            _ = _coordinator.SetHttpContextAsync(invocationId, httpContext);
            _ = _coordinator.RunFunctionInvocationAsync(invocationId);
            await firstSetFunctionTask;
            _coordinator.CompleteFunctionInvocation(invocationId);
        }

        [Fact]
        public async Task FullWorkflow_WithCorrectOrder_CompletesSuccessfully()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var httpContext = new DefaultHttpContext();
            var functionContext = CreateTestFunctionContext();

            // Act - Simulate the full workflow
            var setHttpTask = _coordinator.SetHttpContextAsync(invocationId, httpContext);
            var setFunctionTask = _coordinator.SetFunctionContextAsync(invocationId, functionContext);
            var invocationTask = _coordinator.RunFunctionInvocationAsync(invocationId);

            var resultFunctionContext = await setHttpTask;
            var resultHttpContext = await setFunctionTask;

            _coordinator.CompleteFunctionInvocation(invocationId);
            await invocationTask;

            // Assert
            Assert.Same(functionContext, resultFunctionContext);
            Assert.Same(httpContext, resultHttpContext);
        }

        private static FunctionContext CreateTestFunctionContext(CancellationToken cancellationToken = default)
        {
            var mockContext = new Mock<FunctionContext>();
            mockContext.Setup(c => c.CancellationToken).Returns(cancellationToken);
            mockContext.Setup(c => c.InvocationId).Returns(Guid.NewGuid().ToString());
            return mockContext.Object;
        }

        [Fact]
        public async Task RunFunctionInvocationAsync_WhenCancelled_ThrowsTaskCanceledException()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var httpContext = new DefaultHttpContext();
            var functionContext = CreateTestFunctionContext();
            var cts = new CancellationTokenSource();

            var setHttpTask = _coordinator.SetHttpContextAsync(invocationId, httpContext);
            var setFunctionTask = _coordinator.SetFunctionContextAsync(invocationId, functionContext);

            // Act
            var invocationTask = _coordinator.RunFunctionInvocationAsync(invocationId, cts.Token);

            await setHttpTask;
            await setFunctionTask;

            // Cancel the token (simulates client disconnect)
            cts.Cancel();

            // Assert - The invocation task should be cancelled
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await invocationTask);
        }

        [Fact]
        public async Task RunFunctionInvocationAsync_WhenCancelledAfterCompletion_DoesNotThrow()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var httpContext = new DefaultHttpContext();
            var functionContext = CreateTestFunctionContext();
            var cts = new CancellationTokenSource();

            var setHttpTask = _coordinator.SetHttpContextAsync(invocationId, httpContext);
            var setFunctionTask = _coordinator.SetFunctionContextAsync(invocationId, functionContext);
            var invocationTask = _coordinator.RunFunctionInvocationAsync(invocationId, cts.Token);

            await setHttpTask;
            await setFunctionTask;

            // Complete the function before cancelling
            _coordinator.CompleteFunctionInvocation(invocationId);
            await invocationTask;

            // Act - Cancel after completion should be a no-op
            cts.Cancel();

            // Assert - Task already completed successfully
            Assert.True(invocationTask.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task RunFunctionInvocationAsync_WhenCompletedAfterCancellation_RemainsInCancelledState()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var httpContext = new DefaultHttpContext();
            var functionContext = CreateTestFunctionContext();
            var cts = new CancellationTokenSource();

            var setHttpTask = _coordinator.SetHttpContextAsync(invocationId, httpContext);
            var setFunctionTask = _coordinator.SetFunctionContextAsync(invocationId, functionContext);
            var invocationTask = _coordinator.RunFunctionInvocationAsync(invocationId, cts.Token);

            await setHttpTask;
            await setFunctionTask;

            // Cancel first
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await invocationTask);

            // Act - Complete after cancellation (simulates worker finishing after client disconnects)
            _coordinator.CompleteFunctionInvocation(invocationId);

            // Assert - Task should still be in cancelled state (TrySetResult returns false)
            Assert.True(invocationTask.IsCanceled);
        }

        [Fact]
        public async Task RunFunctionInvocationAsync_WithDefaultToken_CompletesNormally()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var httpContext = new DefaultHttpContext();
            var functionContext = CreateTestFunctionContext();

            var setHttpTask = _coordinator.SetHttpContextAsync(invocationId, httpContext);
            var setFunctionTask = _coordinator.SetFunctionContextAsync(invocationId, functionContext);

            // Act - Use default token (no cancellation)
            var invocationTask = _coordinator.RunFunctionInvocationAsync(invocationId);

            await setHttpTask;
            await setFunctionTask;

            _coordinator.CompleteFunctionInvocation(invocationId);
            await invocationTask;

            // Assert
            Assert.True(invocationTask.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task SetHttpContextAsync_WhenHttpContextTaskFaulted_PropagatesOriginalException()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var httpContext = new DefaultHttpContext();
            var expectedException = new InvalidOperationException("Something went wrong");

            // Pre-create the context reference and fault the TCS
            var contextRef = new ContextReference(invocationId);
            contextRef.HttpContextValueSource.TrySetException(expectedException);

            // Use reflection to insert the faulted context reference
            var field = typeof(DefaultHttpCoordinator)
                .GetField("_contextReferenceList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            var dict = (ConcurrentDictionary<string, ContextReference>)field.GetValue(_coordinator)!;
            dict[invocationId] = contextRef;

            // Act & Assert - Should propagate the original exception
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _coordinator.SetHttpContextAsync(invocationId, httpContext));
            Assert.Same(expectedException, exception);
        }

        [Fact]
        public async Task SetFunctionContextAsync_WhenFunctionContextTaskFaulted_PropagatesOriginalException()
        {
            // Arrange
            string invocationId = Guid.NewGuid().ToString();
            var functionContext = CreateTestFunctionContext();
            var expectedException = new InvalidOperationException("Something went wrong");

            // Pre-create the context reference and fault the TCS
            var contextRef = new ContextReference(invocationId);
            contextRef.FunctionContextValueSource.TrySetException(expectedException);

            // Use reflection to insert the faulted context reference
            var field = typeof(DefaultHttpCoordinator)
                .GetField("_contextReferenceList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            var dict = (ConcurrentDictionary<string, ContextReference>)field.GetValue(_coordinator)!;
            dict[invocationId] = contextRef;

            // Act & Assert - Should propagate the original exception
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _coordinator.SetFunctionContextAsync(invocationId, functionContext));
            Assert.Same(expectedException, exception);
        }
    }
}
