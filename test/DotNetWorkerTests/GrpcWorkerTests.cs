// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Handlers;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Extensions.Logging;
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
        private TestFunctionContext _context = new();
        private TestAsyncFunctionContext _asyncContext = new();
        private ILogger<InvocationHandler> _testLogger;

        public GrpcWorkerTests()
        {
            _mockApplication
                .Setup(m => m.LoadFunction(It.IsAny<FunctionDefinition>()));

            _mockApplication
                .Setup(m => m.CreateContext(It.IsAny<IInvocationFeatures>(), It.IsAny<CancellationToken>()))
                .Returns((IInvocationFeatures f, CancellationToken ct) => {
                    _context = new TestFunctionContext(f, ct);
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
                .Setup(m => m.TryCreate(typeof(DefaultInputConversionFeature), out conversionFeature))
                .Returns(true);

            _testLogger = TestLoggerProvider.Factory.CreateLogger<InvocationHandler>();
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

        [Theory]
        [InlineData(".NET Core 1.0", ".NET Core")]
        [InlineData(".NET Core 1.1", ".NET Core")]
        [InlineData(".NET Core 2.0", ".NET Core")]
        [InlineData(".NET Core 2.1", ".NET Core")]
        [InlineData(".NET Core 2.2", ".NET Core")]
        [InlineData(".NET 3.0", ".NET")]
        [InlineData(".NET 3.1", ".NET")]
        [InlineData(".NET 5", ".NET")]
        [InlineData(".NET 6", ".NET")]
        [InlineData(".NET 7", ".NET")]
        [InlineData(".NET Framework 4.7.1", ".NET Framework")]
        [InlineData(".NET Framework 4.7.2", ".NET Framework")]
        [InlineData(".NET Framework 4.8", ".NET Framework")]
        [InlineData(".NET Framework 4.8.1", ".NET Framework")]
        [InlineData(".NET Native 1.1", ".NET Native")]
        [InlineData(".NET Native 1.2", ".NET Native")]
        [InlineData(".NET Native 1.3", ".NET Native")]
        [InlineData(".NET Native 1.4", ".NET Native")]
        [InlineData(".NET Native 1.6", ".NET Native")]
        [InlineData(".NET Native 2.0", ".NET Native")]
        [InlineData(".NET Native 2.1", ".NET Native")]
        [InlineData("", "")]
        public void FrameworkDescriptionRegex_MatchesExpectedValues(string testFramework, string expectedFramework)
        {
            var result = new Regex(@"^(\D*)+(?!\S)").Match(testFramework).Value;
            Assert.Equal(expectedFramework, result);
        }

        [Fact]
        public void InitRequest_ReturnsExpectedMetadata()
        {
            var response = GrpcWorker.WorkerInitRequestHandler(new(), new WorkerOptions());
            string grpcWorkerVersion = typeof(GrpcWorker).Assembly.GetName().Version?.ToString();
            var runtimeName = new Regex(@"^(\D*)+(?!\S)").Match(RuntimeInformation.FrameworkDescription).Value;

            Assert.Equal(runtimeName, response.WorkerMetadata.RuntimeName);
            Assert.Equal(Environment.Version.ToString(), response.WorkerMetadata.RuntimeVersion);
            Assert.Equal(WorkerInformation.Instance.WorkerVersion, response.WorkerMetadata.WorkerVersion);
            Assert.Equal(RuntimeInformation.ProcessArchitecture.ToString(), response.WorkerMetadata.WorkerBitness);

            Assert.Contains(response.WorkerMetadata.CustomProperties,
                kvp => string.Equals(kvp.Key, "Worker.Grpc.Version", StringComparison.OrdinalIgnoreCase)
                && string.Equals(kvp.Value, grpcWorkerVersion, StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("IncludeEmptyEntriesInMessagePayload", true, "IncludeEmptyEntriesInMessagePayload", true, "True")]
        [InlineData("IncludeEmptyEntriesInMessagePayload", false, "IncludeEmptyEntriesInMessagePayload", false)]
        public void InitRequest_ReturnsExpectedCapabilities_BasedOnWorkerOptions(
            string booleanPropertyName,
            bool booleanPropertyValue,
            string capabilityName,
            bool shouldCapabilityPresent,
            string expectedCapabilityValue = null)
        {
            var workerOptions = new WorkerOptions();
            // Update boolean property values of workerOption based on test input parameters.
            workerOptions.GetType().GetProperty(booleanPropertyName)?.SetValue(workerOptions, booleanPropertyValue);

            var response = GrpcWorker.WorkerInitRequestHandler(new(), workerOptions);

            IDictionary<string, string> capabilitiesDict = response.Capabilities;
            Assert.Same(bool.TrueString, capabilitiesDict["RpcHttpBodyOnly"]);
            Assert.Same(bool.TrueString, capabilitiesDict["RawHttpBodyBytes"]);
            Assert.Same(bool.TrueString, capabilitiesDict["RpcHttpTriggerMetadataRemoved"]);
            Assert.Same(bool.TrueString, capabilitiesDict["UseNullableValueDictionaryForHttp"]);
            Assert.Same(bool.TrueString, capabilitiesDict["TypedDataCollection"]);
            Assert.Same(bool.TrueString, capabilitiesDict["WorkerStatus"]);
            Assert.Same(bool.TrueString, capabilitiesDict["HandlesWorkerTerminateMessage"]);
            Assert.Same(bool.TrueString, capabilitiesDict["HandlesInvocationCancelMessage"]);

            if (shouldCapabilityPresent)
            {
                Assert.Contains(capabilityName, capabilitiesDict);
                Assert.Equal(expectedCapabilityValue, capabilitiesDict[capabilityName]);
            }
            else
            {
                Assert.DoesNotContain(capabilityName, capabilitiesDict);
            }
        }

        [Fact]
        public async Task Invoke_ReturnsSuccess()
        {
            var request = TestUtility.CreateInvocationRequest();

            var invocationHandler = new InvocationHandler(_mockApplication.Object,
                _mockFeaturesFactory.Object, new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object,
                _mockInputConversionFeatureProvider.Object, _testLogger);

            var response = await invocationHandler.InvokeAsync(request);

            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
            Assert.True(_context.IsDisposed);
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

            var invocationHandler = new InvocationHandler(_mockApplication.Object,
                _mockFeaturesFactory.Object, new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object,
                _mockInputConversionFeatureProvider.Object, _testLogger);

            var response = await invocationHandler.InvokeAsync(request);

            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
            Assert.True((_context as TestAsyncFunctionContext).IsAsyncDisposed);
            Assert.True(_context.IsDisposed);
        }

        [Fact]
        public async Task Invoke_SetsRetryContext()
        {
            var request = TestUtility.CreateInvocationRequest();

            var invocationHandler = new InvocationHandler(_mockApplication.Object,
                _mockFeaturesFactory.Object, new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object,
                _mockInputConversionFeatureProvider.Object, _testLogger);

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

            var invocationHandler = new InvocationHandler(_mockApplication.Object,
                _mockFeaturesFactory.Object, new JsonObjectSerializer(), _mockOutputBindingsInfoProvider.Object,
                _mockInputConversionFeatureProvider.Object, _testLogger);

            var response = await invocationHandler.InvokeAsync(request);

            Assert.Equal(StatusResult.Types.Status.Failure, response.Result.Status);
            Assert.Contains("InvalidOperationException: whoops", response.Result.Exception.Message);
            Assert.Contains("CreateContext", response.Result.Exception.Message);
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

        // Used for MethodInfo in tests
        private void TestRun()
        {
        }
    }
}
