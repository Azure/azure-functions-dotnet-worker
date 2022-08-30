﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Handlers;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class GrpcWorkerTests
    {
        private readonly Mock<IFunctionsApplication> _mockApplication = new(MockBehavior.Strict);
        private readonly Mock<IInvocationFeaturesFactory> _mockFeaturesFactory = new(MockBehavior.Strict);
        private readonly Mock<IInputConversionFeatureProvider> _mockInputConversionFeatureProvider = new(MockBehavior.Strict);
        private readonly Mock<IInputConversionFeature> mockConversionFeature = new(MockBehavior.Strict);
        private readonly Mock<IOutputBindingsInfoProvider> _mockOutputBindingsInfoProvider = new(MockBehavior.Strict);
        private readonly Mock<IMethodInfoLocator> _mockMethodInfoLocator = new(MockBehavior.Strict);
        private readonly TestLoggerProvider _testLoggerProvider = new();
        private TestFunctionContext _context = new();
        private TestAsyncFunctionContext _asyncContext = new();

        public GrpcWorkerTests()
        {
            _mockApplication
                .Setup(m => m.LoadFunction(It.IsAny<FunctionDefinition>()));

            _mockApplication
                .Setup(m => m.CreateContext(It.IsAny<IInvocationFeatures>(), default))
                .Returns<IInvocationFeatures>(f =>
                {
                    _context = new TestFunctionContext(f);
                    return _context;
                });

            _mockApplication
                .Setup(m => m.InvokeFunctionAsync(It.IsAny<FunctionContext>()))
                .Returns(Task.CompletedTask);

            _mockFeaturesFactory
                .Setup(m => m.Create())
                .Returns(new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>()));

            _mockMethodInfoLocator
                .Setup(m => m.GetMethod(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(typeof(GrpcWorkerTests).GetMethod(nameof(TestRun), BindingFlags.Instance | BindingFlags.NonPublic));

            IInputConversionFeature conversionFeature = mockConversionFeature.Object;
            _mockInputConversionFeatureProvider
                .Setup(m=>m.TryCreate(typeof(DefaultInputConversionFeature), out conversionFeature))
                .Returns(true);
        }

        [Fact]
        public void LoadFunction_ReturnsSuccess()
        {
            FunctionLoadRequest request = CreateFunctionLoadRequest();

            var response = GrpcWorker.FunctionLoadRequestHandler(request, _mockApplication.Object, _mockMethodInfoLocator.Object);

            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
        }

        [Fact]
        public void LoadFunction_WithProxyMetadata_ReturnsSuccess()
        {
            FunctionLoadRequest request = CreateFunctionLoadRequest();

            request.Metadata.IsProxy = true;

            var response = GrpcWorker.FunctionLoadRequestHandler(request, _mockApplication.Object, _mockMethodInfoLocator.Object);

            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
        }

        [Fact]
        public void LoadFunction_Throws_ReturnsFailure()
        {
            _mockApplication
                .Setup(m => m.LoadFunction(It.IsAny<FunctionDefinition>()))
                .Throws(new InvalidOperationException("whoops"));

            FunctionLoadRequest request = CreateFunctionLoadRequest();

            var response = GrpcWorker.FunctionLoadRequestHandler(request, _mockApplication.Object, _mockMethodInfoLocator.Object);

            Assert.Equal(StatusResult.Types.Status.Failure, response.Result.Status);
            Assert.Contains("InvalidOperationException: whoops", response.Result.Exception.Message);
            Assert.Contains("LoadFunction", response.Result.Exception.Message);
        }

        [Fact]
        public void MethodInfoLocator_Throws_ReturnsFailure()
        {
            _mockMethodInfoLocator
                .Setup(m => m.GetMethod(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException("whoops"));

            FunctionLoadRequest request = CreateFunctionLoadRequest();

            var response = GrpcWorker.FunctionLoadRequestHandler(request, _mockApplication.Object, _mockMethodInfoLocator.Object);

            Assert.Equal(StatusResult.Types.Status.Failure, response.Result.Status);
            Assert.Contains("InvalidOperationException: whoops", response.Result.Exception.Message);
            Assert.Contains("GetMethod", response.Result.Exception.Message);
        }

        [Fact]
        public void InitRequest_ReturnsExpectedMetadata()
        {
            var response = GrpcWorker.WorkerInitRequestHandler(new());

            string grpcWorkerVersion = typeof(GrpcWorker).Assembly.GetName().Version?.ToString();
            Assert.Equal(RuntimeInformation.FrameworkDescription, response.WorkerMetadata.RuntimeName);
            Assert.Equal(Environment.Version.ToString(), response.WorkerMetadata.RuntimeVersion);
            Assert.Equal(WorkerInformation.Instance.WorkerVersion, response.WorkerMetadata.WorkerVersion);
            Assert.Equal(RuntimeInformation.ProcessArchitecture.ToString(), response.WorkerMetadata.WorkerBitness);
            Assert.Contains(response.WorkerMetadata.CustomProperties,
                kvp => string.Equals(kvp.Key, "Worker.Grpc.Version", StringComparison.OrdinalIgnoreCase)
                && string.Equals(kvp.Value, grpcWorkerVersion, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Invoke_ReturnsSuccess()
        {
            var request = CreateInvocationRequest();

            var invocationHandler = new InvocationHandler(_mockApplication.Object,
                _mockFeaturesFactory.Object, new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object,
                _mockInputConversionFeatureProvider.Object, _testLoggerProvider);

            var response = await invocationHandler.InvokeAsync(request);

            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
            Assert.True(_context.IsDisposed);
        }

        [Fact]
        public async Task Invoke_ReturnsSuccess_AsyncFunctionContext()
        {
            var request = CreateInvocationRequest();

            // Mock IFunctionApplication.CreateContext to return TestAsyncFunctionContext instance.
            _mockApplication
                .Setup(m => m.CreateContext(It.IsAny<IInvocationFeatures>(), default))
                .Returns<IInvocationFeatures>(f =>
                {
                    _context = new TestAsyncFunctionContext(f);
                    return _context;
                });

            var invocationHandler = new InvocationHandler(_mockApplication.Object,
                _mockFeaturesFactory.Object, new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object,
                _mockInputConversionFeatureProvider.Object, _testLoggerProvider);

            var response = await invocationHandler.InvokeAsync(request);

            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
            Assert.True((_context as TestAsyncFunctionContext).IsAsyncDisposed);
            Assert.True(_context.IsDisposed);
        }

        [Fact]
        public async Task Invoke_SetsRetryContext()
        {
            var request = CreateInvocationRequest();

            var invocationHandler = new InvocationHandler(_mockApplication.Object,
                _mockFeaturesFactory.Object, new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object,
                _mockInputConversionFeatureProvider.Object, _testLoggerProvider);

            var response = await invocationHandler.InvokeAsync(request);

            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
            Assert.True(_context.IsDisposed);
            Assert.Equal(request.RetryContext.RetryCount, _context.RetryContext.RetryCount);
            Assert.Equal(request.RetryContext.MaxRetryCount, _context.RetryContext.MaxRetryCount);
        }

        [Fact]
        public async Task Invoke_CreateContext_WithCancellationTokenParameter_CreatesCancellationSource()
        {
            _mockApplication
                .Setup(m => m.CreateContext(It.IsAny<IInvocationFeatures>(), default))
                .Throws(new InvalidOperationException("whoops"));

            var request = CreateInvocationRequest();

            var invocationHandler = new InvocationHandler(_mockApplication.Object,
                _mockFeaturesFactory.Object, new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object,
                _mockInputConversionFeatureProvider.Object, _testLoggerProvider);

            var response = await invocationHandler.InvokeAsync(request);

            Assert.Equal(StatusResult.Types.Status.Failure, response.Result.Status);
            Assert.Contains("InvalidOperationException: whoops", response.Result.Exception.Message);
            Assert.Contains("CreateContext", response.Result.Exception.Message);
        }

        [Fact]
        public async Task Invoke_CreateContextThrows_ReturnsFailure()
        {
            _mockApplication
                .Setup(m => m.CreateContext(It.IsAny<IInvocationFeatures>(), default))
                .Throws(new InvalidOperationException("whoops"));

            var request = CreateInvocationRequest();

            var invocationHandler = new InvocationHandler(_mockApplication.Object,
                _mockFeaturesFactory.Object, new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object,
                _mockInputConversionFeatureProvider.Object, _testLoggerProvider);

            var response = await invocationHandler.InvokeAsync(request);

            Assert.Equal(StatusResult.Types.Status.Failure, response.Result.Status);
            Assert.Contains("InvalidOperationException: whoops", response.Result.Exception.Message);
            Assert.Contains("CreateContext", response.Result.Exception.Message);
        }

        [Fact]
        public async Task Invoke_CreatesCancellationToken()
        {
            _mockApplication
                .Setup(m => m.InvokeFunctionAsync(It.IsAny<FunctionContext>()))
                .Throws(new AggregateException(new Exception[] { new TaskCanceledException() }));

            var request = CreateInvocationRequest();

            var invocationHandler = new InvocationHandler(_mockApplication.Object,
                _mockFeaturesFactory.Object, new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object,
                _mockInputConversionFeatureProvider.Object, _testLoggerProvider);

            var response = await invocationHandler.InvokeAsync(request);

            Assert.Equal(StatusResult.Types.Status.Cancelled, response.Result.Status);
            Assert.Contains("TaskCanceledException", response.Result.Exception.Message);
        }

        [Fact]
        public async Task Invoke_ThrowsTaskCanceledException_ReturnsCancelled()
        {
            _mockApplication
                .Setup(m => m.InvokeFunctionAsync(It.IsAny<FunctionContext>()))
                .Throws(new AggregateException(new Exception[] { new TaskCanceledException() }));

            var request = CreateInvocationRequest();

            var invocationHandler = new InvocationHandler(_mockApplication.Object,
                _mockFeaturesFactory.Object, new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object,
                _mockInputConversionFeatureProvider.Object, _testLoggerProvider);

            var response = await invocationHandler.InvokeAsync(request);

            Assert.Equal(StatusResult.Types.Status.Cancelled, response.Result.Status);
            Assert.Contains("TaskCanceledException", response.Result.Exception.Message);
        }

        private static FunctionLoadRequest CreateFunctionLoadRequest()
        {
            return new FunctionLoadRequest
            {
                Metadata = new RpcFunctionMetadata
                {
                    ScriptFile = "DoesNotMatter.dll"
                }
            };
        }

        private static InvocationRequest CreateInvocationRequest()
        {
            return new InvocationRequest
            {
                TraceContext = new RpcTraceContext
                {
                    TraceParent = Guid.NewGuid().ToString(),
                    TraceState = Guid.NewGuid().ToString()
                },
                RetryContext = new Grpc.Messages.RetryContext
                {
                    MaxRetryCount = 3,
                    RetryCount = 2
                }
            };
        }

        // Used for MethodInfo in tests
        private void TestRun()
        {
        }
    }
}
