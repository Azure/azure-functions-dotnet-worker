// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Handlers;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class InvocationHandlerTests
    {
        private readonly Mock<IFunctionsApplication> _mockApplication = new(MockBehavior.Strict);
        private readonly Mock<IInvocationFeaturesFactory> _mockInvocationFeaturesFactory = new(MockBehavior.Strict);
        private readonly Mock<IOutputBindingsInfoProvider> _mockOutputBindingsInfoProvider = new(MockBehavior.Strict);
        private readonly Mock<IInputConversionFeatureProvider> _mockInputConversionFeatureProvider = new(MockBehavior.Strict);
        private readonly Mock<IInputConversionFeature> mockConversionFeature = new(MockBehavior.Strict);
        private TestFunctionContext _context = new();
        private ILogger<InvocationHandler> _testLogger;

        public InvocationHandlerTests()
        {
            _mockApplication
                .Setup(m => m.CreateContext(It.IsAny<IInvocationFeatures>(), It.IsAny<CancellationToken>()))
                .Returns((IInvocationFeatures f, CancellationToken ct) =>
                {
                    _context = new TestFunctionContext(f, ct);
                    return _context;
                });

            _mockApplication
                .Setup(m => m.InvokeFunctionAsync(It.IsAny<FunctionContext>()))
                .Returns(Task.CompletedTask);

            _mockInvocationFeaturesFactory
                .Setup(m => m.Create())
                .Returns(new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>()));

            IInputConversionFeature conversionFeature = mockConversionFeature.Object;
            _mockInputConversionFeatureProvider
                .Setup(m => m.TryCreate(typeof(DefaultInputConversionFeature), out conversionFeature))
                .Returns(true);

            _testLogger = TestLoggerProvider.Factory.CreateLogger<InvocationHandler>();
        }

        [Fact]
        public async Task InvokeAsync_CreatesValidCancellationToken_ReturnsSuccess()
        {
            var invocationId = "5fb3a9b4-0b38-450a-9d46-35946e7edea7";
            var request = TestUtility.CreateInvocationRequest(invocationId);

            var handler = new InvocationHandler(_mockApplication.Object,
                _mockInvocationFeaturesFactory.Object, new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object,
                _mockInputConversionFeatureProvider.Object, _testLogger);

            var response = await handler.InvokeAsync(request);

            // InvokeAsync should create a real cancellation token which can be cancelled,
            // otherwise we set the token to be CancellationToken.Empty which **cannot** be cancelled
            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
            Assert.True(_context.CancellationToken.CanBeCanceled);
        }

        [Fact]
        public async Task InvokeAsync_ThrowsTaskCanceledException_ReturnsCancelled()
        {
            _mockApplication
                .Setup(m => m.InvokeFunctionAsync(It.IsAny<FunctionContext>()))
                .Throws(new AggregateException(new Exception[] { new TaskCanceledException() }));

            var request = TestUtility.CreateInvocationRequest("abc");

            var invocationHandler = new InvocationHandler(_mockApplication.Object,
                _mockInvocationFeaturesFactory.Object, new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object,
                _mockInputConversionFeatureProvider.Object, _testLogger);

            var response = await invocationHandler.InvokeAsync(request);

            Assert.Equal(StatusResult.Types.Status.Cancelled, response.Result.Status);
            Assert.Contains("TaskCanceledException", response.Result.Exception.Message);
        }

        [Fact]
        public void Cancel_InvocationInProgress_CancelsTokenSource_ReturnsTrue()
        {
            var invocationId = "5fb3a9b4-0b38-450a-9d46-35946e7edea7";
            var request = TestUtility.CreateInvocationRequest(invocationId);
            var cts = new CancellationTokenSource();

            var handler = new InvocationHandler(_mockApplication.Object,
                _mockInvocationFeaturesFactory.Object, new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object,
                _mockInputConversionFeatureProvider.Object, _testLogger);

            // Mock delay in InvokeFunctionAsync so that we can cancel mid invocation
            _mockApplication
                .Setup(m => m.InvokeFunctionAsync(It.IsAny<FunctionContext>()))
                .Callback(() => Thread.Sleep(1000))
                .Returns(Task.CompletedTask);

            // Don't wait for InvokeAsync so we can cancel whilst it's in progress
            _ = Task.Run(async () =>
            {
                await handler.InvokeAsync(request);
            });

            // Buffer to ensure the cancellation token source was created before we try to cancel
            Thread.Sleep(500);

            var result = handler.TryCancel(invocationId);
            Assert.True(result);
        }

        [Fact]
        public async Task Cancel_InvocationCompleted_ReturnsFalse()
        {
            var invocationId = "5fb3a9b4-0b38-450a-9d46-35946e7edea7";
            var request = TestUtility.CreateInvocationRequest(invocationId);

            var handler = new InvocationHandler(_mockApplication.Object,
                _mockInvocationFeaturesFactory.Object, new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object,
                _mockInputConversionFeatureProvider.Object, _testLogger);

            _ = await handler.InvokeAsync(request);

            var result = handler.TryCancel(invocationId);
            Assert.False(result);
        }

        /// <summary>
        /// Test unwrapping user code exception functionality. 
        /// </summary>
        [Fact]
        public async Task InvokeAsync_UserCodeThrowsException_OptionEnabled()
        {
            var exceptionMessage = "user code exception";
            var mockOptions = new WorkerOptions()
            {
                EnableUserCodeException = true
            };

            _mockApplication
                .Setup(m => m.InvokeFunctionAsync(It.IsAny<FunctionContext>()))
                .Throws(new Exception(exceptionMessage));
            var request = TestUtility.CreateInvocationRequest("abc");

            var invocationHandler = new InvocationHandler(_mockApplication.Object,
                _mockInvocationFeaturesFactory.Object, new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object,
                _mockInputConversionFeatureProvider.Object, _testLogger);

            var response = await invocationHandler.InvokeAsync(request, mockOptions);

            Assert.Equal(StatusResult.Types.Status.Failure, response.Result.Status);
            Assert.Equal("System.Exception", response.Result.Exception.Type.ToString());
            Assert.Equal(exceptionMessage, response.Result.Exception.Message);
            Assert.True(response.Result.Exception.IsUserException);
        }

        /// <summary>
        /// Test keeping user code exception wrapped as RpcException. 
        /// </summary>
        [Fact]
        public async Task InvokeAsync_UserCodeThrowsException_OptionDisabled()
        {
            var exceptionMessage = "user code exception";
            var mockOptions = new WorkerOptions()
            {
                EnableUserCodeException = false
            };

            _mockApplication
                .Setup(m => m.InvokeFunctionAsync(It.IsAny<FunctionContext>()))
                .Throws(new Exception(exceptionMessage));
            var request = TestUtility.CreateInvocationRequest("abc");

            var invocationHandler = new InvocationHandler(_mockApplication.Object,
                _mockInvocationFeaturesFactory.Object, new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object,
                _mockInputConversionFeatureProvider.Object, _testLogger);

            var response = await invocationHandler.InvokeAsync(request, mockOptions);

            Assert.Equal(StatusResult.Types.Status.Failure, response.Result.Status);
            Assert.NotEqual("System.Exception", response.Result.Exception.Type.ToString());
            Assert.NotEqual(exceptionMessage, response.Result.Exception.Message);
            Assert.False(response.Result.Exception.IsUserException);
        }
    }
}
