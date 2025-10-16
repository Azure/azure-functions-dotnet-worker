// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Invocation;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Pipeline.Tests
{
    public class FunctionsExecutionMiddlewareTests
    {
        [Fact]
        public void Ctor_ThrowsArgumentNullException_WhenFunctionExecutorIsNull()
        {
            // Arrange & Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new FunctionExecutionMiddleware(null!));
            Assert.Equal("functionExecutor", exception.ParamName);
        }

        [Fact]
        public async Task Invoke_ThrowsArgumentNullException_WhenFunctionContextIsNull()
        {
            // Arrange
            FunctionExecutionMiddleware middleware = new(Mock.Of<IFunctionExecutor>());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => middleware.Invoke(null!));
            Assert.Equal("context", exception.ParamName);
        }

        [Fact]
        public async Task Invoke_NoFeature_CallsInjectedExecutor()
        {
            // Arrange
            Mock<IFunctionExecutor> functionExecutorMock = new(MockBehavior.Strict);
            FunctionExecutionMiddleware middleware = new(functionExecutorMock.Object);
            Mock<FunctionContext> functionContextMock = new(MockBehavior.Strict);
            functionContextMock.Setup(m => m.Features).Returns(new InvocationFeatures([]));

            functionExecutorMock
                .Setup(m => m.ExecuteAsync(functionContextMock.Object))
                .Returns(ValueTask.CompletedTask)
                .Verifiable();

            // Act
            await middleware.Invoke(functionContextMock.Object);

            // Assert
            functionExecutorMock.Verify(f => f.ExecuteAsync(functionContextMock.Object), Times.Once);
        }

        [Fact]
        public async Task Invoke_Feature_CallsFeatureExecutor()
        {
            // Arrange
            Mock<IFunctionExecutor> functionExecutorMock1 = new(MockBehavior.Strict);
            Mock<IFunctionExecutor> functionExecutorMock2 = new(MockBehavior.Strict);
            FunctionExecutionMiddleware middleware = new(functionExecutorMock1.Object);
            Mock<FunctionContext> functionContextMock = new(MockBehavior.Strict);

            InvocationFeatures features = new([]);
            features.Set(functionExecutorMock2.Object);
            functionContextMock.Setup(m => m.Features).Returns(features);

            functionExecutorMock2
                .Setup(m => m.ExecuteAsync(functionContextMock.Object))
                .Returns(ValueTask.CompletedTask)
                .Verifiable();

            // Act
            await middleware.Invoke(functionContextMock.Object);

            // Assert
            functionExecutorMock1.Verify(f => f.ExecuteAsync(It.IsAny<FunctionContext>()), Times.Never);
            functionExecutorMock2.Verify(f => f.ExecuteAsync(functionContextMock.Object), Times.Once);
        }
    }
}
