// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
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
        private readonly Mock<IOutputBindingsInfoProvider> _mockOutputBindingsInfoProvider = new(MockBehavior.Strict);
        private readonly Mock<IMethodInfoLocator> _mockMethodInfoLocator = new(MockBehavior.Strict);
        private TestFunctionContext _context = new();

        public GrpcWorkerTests()
        {
            _mockApplication
                .Setup(m => m.LoadFunction(It.IsAny<FunctionDefinition>()));

            _mockApplication
                .Setup(m => m.CreateContext(It.IsAny<IInvocationFeatures>()))
                .Returns(_context);

            _mockApplication
                .Setup(m => m.InvokeFunctionAsync(It.IsAny<FunctionContext>()))
                .Returns(Task.CompletedTask);

            _mockFeaturesFactory
                .Setup(m => m.Create())
                .Returns(new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>()));

            _mockMethodInfoLocator
                .Setup(m => m.GetMethod(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(typeof(GrpcWorkerTests).GetMethod(nameof(TestRun), BindingFlags.Instance | BindingFlags.NonPublic));
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
        public async Task Invoke_ReturnsSuccess()
        {
            var request = CreateInvocationRequest();

            var response = await GrpcWorker.InvocationRequestHandlerAsync(request, _mockApplication.Object, _mockFeaturesFactory.Object,
                new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object);

            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
            Assert.True(_context.IsDisposed);
        }

        [Fact]
        public async Task Invoke_CreateContextThrows_ReturnsFailure()
        {
            _mockApplication
                .Setup(m => m.CreateContext(It.IsAny<IInvocationFeatures>()))
                .Throws(new InvalidOperationException("whoops"));

            var request = CreateInvocationRequest();

            var response = await GrpcWorker.InvocationRequestHandlerAsync(request, _mockApplication.Object, _mockFeaturesFactory.Object,
                new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object);

            Assert.Equal(StatusResult.Types.Status.Failure, response.Result.Status);
            Assert.Contains("InvalidOperationException: whoops", response.Result.Exception.Message);
            Assert.Contains("CreateContext", response.Result.Exception.Message);
        }

        [Fact]
        public async Task Invoke_InvokeAsyncThrows_ReturnsFailure()
        {
            _mockApplication
                .Setup(m => m.InvokeFunctionAsync(It.IsAny<FunctionContext>()))
                .Throws(new InvalidOperationException("whoops"));

            var request = CreateInvocationRequest();

            var response = await GrpcWorker.InvocationRequestHandlerAsync(request, _mockApplication.Object, _mockFeaturesFactory.Object,
                new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object);

            Assert.Equal(StatusResult.Types.Status.Failure, response.Result.Status);
            Assert.Contains("InvalidOperationException: whoops", response.Result.Exception.Message);
            Assert.Contains("InvokeFunctionAsync", response.Result.Exception.Message);
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
                }
            };
        }

        // Used for MethodInfo in tests
        private void TestRun()
        {
        }
    }
}
