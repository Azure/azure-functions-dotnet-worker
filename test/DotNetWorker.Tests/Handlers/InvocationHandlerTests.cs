// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.ApplicationInsights;
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
        private readonly Mock<IInvocationFeaturesFactory> _mockFeaturesFactory = new(MockBehavior.Strict);
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

            _mockFeaturesFactory
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
            var handler = CreateInvocationHandler();
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
                .Throws(new AggregateException(new TaskCanceledException()));

            var request = TestUtility.CreateInvocationRequest("abc");
            var invocationHandler = CreateInvocationHandler();
            var response = await invocationHandler.InvokeAsync(request);

            Assert.Equal(StatusResult.Types.Status.Cancelled, response.Result.Status);
            Assert.Equal("System.AggregateException", response.Result.Exception.Type);
            Assert.Contains("A task was canceled", response.Result.Exception.Message);
        }

        [Fact]
        public void Cancel_InvocationInProgress_CancelsTokenSource_ReturnsTrue()
        {
            var invocationId = "5fb3a9b4-0b38-450a-9d46-35946e7edea7";
            var request = TestUtility.CreateInvocationRequest(invocationId);
            var cts = new CancellationTokenSource();

            // Mock delay in InvokeFunctionAsync so that we can cancel mid invocation
            _mockApplication
                .Setup(m => m.InvokeFunctionAsync(It.IsAny<FunctionContext>()))
                .Callback(() => Thread.Sleep(1000))
                .Returns(Task.CompletedTask);

            var invocationHandler = CreateInvocationHandler();

            // Don't wait for InvokeAsync so we can cancel whilst it's in progress
            _ = Task.Run(async () =>
            {
                await invocationHandler.InvokeAsync(request);
            });

            // Buffer to ensure the cancellation token source was created before we try to cancel
            Thread.Sleep(500);

            var result = invocationHandler.TryCancel(invocationId);
            Assert.True(result);
        }

        [Fact]
        public async Task Cancel_InvocationCompleted_ReturnsFalse()
        {
            var invocationId = "5fb3a9b4-0b38-450a-9d46-35946e7edea7";
            var request = TestUtility.CreateInvocationRequest(invocationId);
            var invocationHandler = CreateInvocationHandler();

            _ = await invocationHandler.InvokeAsync(request);

            var result = invocationHandler.TryCancel(invocationId);
            Assert.False(result);
        }

        /// <summary>
        /// Test unwrapping user code exception functionality.
        /// EnableUserCodeException capability is true by default.
        /// </summary>
        [Fact]
        public async Task InvokeAsync_UserCodeThrowsException_OptionEnabled()
        {
            var exceptionMessage = "user code exception";

            var mockOptions = new OptionsWrapper<WorkerOptions>(new()
            {
                Serializer = new JsonObjectSerializer()
            });

            _mockApplication
                .Setup(m => m.InvokeFunctionAsync(It.IsAny<FunctionContext>()))
                .Throws(new Exception(exceptionMessage));

            var request = TestUtility.CreateInvocationRequest("abc");
            var invocationHandler = CreateInvocationHandler(workerOptions: mockOptions);
            var response = await invocationHandler.InvokeAsync(request);

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
#pragma warning disable CS0618 // Type or member is obsolete. Test obsolete property.
            var mockOptions = new OptionsWrapper<WorkerOptions>(new()
            {
                Serializer = new JsonObjectSerializer(),
                EnableUserCodeException = false
            });
#pragma warning restore CS0618 // Type or member is obsolete

            _mockApplication
                .Setup(m => m.InvokeFunctionAsync(It.IsAny<FunctionContext>()))
                .Throws(new Exception(exceptionMessage));

            var request = TestUtility.CreateInvocationRequest("abc");
            var invocationHandler = CreateInvocationHandler(workerOptions: mockOptions);
            var response = await invocationHandler.InvokeAsync(request);

            Assert.Equal(StatusResult.Types.Status.Failure, response.Result.Status);
            Assert.NotEqual("System.Exception", response.Result.Exception.Type.ToString());
            Assert.NotEqual(exceptionMessage, response.Result.Exception.Message);
            Assert.False(response.Result.Exception.IsUserException);
        }

        [Fact]
        public async Task Invoke_ReturnsSuccess_AsyncFunctionContext()
        {
            var request = TestUtility.CreateInvocationRequest();

            // Mock IFunctionApplication.CreateContext to return TestAsyncFunctionContext instance.
            _mockApplication
                .Setup(m => m.CreateContext(It.IsAny<IInvocationFeatures>(), It.IsAny<CancellationToken>()))
                .Returns<IInvocationFeatures, CancellationToken>((f, ct) =>
                {
                    _context = new TestAsyncFunctionContext(f);
                    return _context;
                });

            var invocationHandler = CreateInvocationHandler();
            var response = await invocationHandler.InvokeAsync(request);

            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
            Assert.True((_context as TestAsyncFunctionContext).IsAsyncDisposed);
            Assert.True(_context.IsDisposed);
        }

        [Fact]
        public async Task Invoke_ReturnsSuccess()
        {
            var request = TestUtility.CreateInvocationRequest();
            var invocationHandler = CreateInvocationHandler();
            var response = await invocationHandler.InvokeAsync(request);

            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
            Assert.True(_context.IsDisposed);
        }

        [Fact]
        public async Task Invoke_SetsRetryContext()
        {
            var request = TestUtility.CreateInvocationRequest();
            var invocationHandler = CreateInvocationHandler();
            var response = await invocationHandler.InvokeAsync(request);

            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
            Assert.True(_context.IsDisposed);
            Assert.Equal(request.RetryContext.RetryCount, _context.RetryContext.RetryCount);
            Assert.Equal(request.RetryContext.MaxRetryCount, _context.RetryContext.MaxRetryCount);
        }

        [Fact]
        public async Task Invoke_CreateContextThrows_ReturnsFailure()
        {
            _mockApplication
                .Setup(m => m.CreateContext(It.IsAny<IInvocationFeatures>(), It.IsAny<CancellationToken>()))
                .Throws(new InvalidOperationException("whoops"));

            var request = TestUtility.CreateInvocationRequest();
            var invocationHandler = CreateInvocationHandler();
            var response = await invocationHandler.InvokeAsync(request);

            Assert.Equal(StatusResult.Types.Status.Failure, response.Result.Status);
            Assert.Equal("System.InvalidOperationException", response.Result.Exception.Type);
            Assert.Contains("whoops", response.Result.Exception.Message);
            Assert.Contains("CreateContext", response.Result.Exception.StackTrace);
        }

        [Fact]
        public async Task SetRetryContextToNull()
        {
            var request = TestUtility.CreateInvocationRequestWithNullRetryContext();
            var invocationHandler = CreateInvocationHandler();
            var response = await invocationHandler.InvokeAsync(request);

            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
            Assert.True(_context.IsDisposed);
            Assert.Null(_context.RetryContext);
        }

        [Fact]
        public async Task InvokeAsync_TagsInFunctionInvocationContextItems_AreIncludedInInvocationResponse()
        {
            // Arrange
            var invocationId = "tags-test-invocation";
            var request = TestUtility.CreateInvocationRequest(invocationId);

            // Build tags dictionary the same way as Activity and FunctionsApplication would
            var activityTags = new List<KeyValuePair<string, string>>
            {
                new("customTag1", "value1"),
                new("customTag2", "value2"),
                new("customTag1", "value1-latest")
            };

            var expectedTags = activityTags
                .GroupBy(kv => kv.Key)
                .ToDictionary(g => g.Key, g => g.Last().Value);

            // Set up the application to add tags to the context.Items
            _mockApplication
                .Setup(m => m.InvokeFunctionAsync(It.IsAny<FunctionContext>()))
                .Callback<FunctionContext>(ctx =>
                {
                    // Ensure Items is initialized before use
                    ctx.Items ??= new System.Collections.Generic.Dictionary<object, object>();

                    // Simulate tags being set in the Items dictionary
                    ctx.Items[Worker.Diagnostics.TraceConstants.InternalKeys.FunctionContextItemsKey] =
                        new Dictionary<string, string>(expectedTags);
                })
                .Returns(Task.CompletedTask);

            var handler = CreateInvocationHandler();

            // Act
            var response = await handler.InvokeAsync(request);

            // Assert
            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);

            // Extract tags from the response
            var tags = response.TraceContext?.Attributes;
            Assert.NotNull(tags);
            foreach (var expectedTag in expectedTags)
            {
                var tag = tags.FirstOrDefault(t => t.Key == expectedTag.Key);
                Assert.Equal(expectedTag.Value, tag.Value);
            }
        }

        private InvocationHandler CreateInvocationHandler(IFunctionsApplication application = null,
                                                          IOptions<WorkerOptions> workerOptions = null)
        {
            workerOptions ??= CreateDefaultWorkerOptions();

            return new InvocationHandler(application ?? _mockApplication.Object,
                _mockFeaturesFactory.Object, _mockOutputBindingsInfoProvider.Object,
                _mockInputConversionFeatureProvider.Object, workerOptions, _testLogger);
        }
        private static IOptions<WorkerOptions> CreateDefaultWorkerOptions()
        {
            return new OptionsWrapper<WorkerOptions>(new()
            {
                Serializer = new JsonObjectSerializer()
            });
        }
    }
}
