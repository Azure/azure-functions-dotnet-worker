// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Tests;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Handlers;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class GrpcWorkerTests
    {
        private readonly Mock<IFunctionsApplication> _mockApplication = new(MockBehavior.Strict);
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
                .Returns((IInvocationFeatures f, CancellationToken ct) =>
                {
                    _context = new TestFunctionContext(f, ct);
                    return _context;
                });

            _mockApplication
                .Setup(m => m.InvokeFunctionAsync(It.IsAny<FunctionContext>()))
                .Returns(Task.CompletedTask);

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
            var attr = Assembly.GetExecutingAssembly().GetCustomAttribute<TargetFrameworkAttribute>();
            using var testVariables = new TestScopedEnvironmentVariable("FUNCTIONS_WORKER_DIRECTORY", "test");

            FunctionLoadRequest request = CreateFunctionLoadRequest();

            var response = GrpcWorker.FunctionLoadRequestHandler(request, _mockApplication.Object, _mockMethodInfoLocator.Object);

            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
        }

        [Theory]
        [InlineData(".NET Core 3.1.1", ".NET Core")]
        [InlineData(".NET 8.0.0", ".NET")]
        [InlineData(".NET Framework 4.8.4250.0", ".NET Framework")]
        [InlineData(".NET Native", ".NET Native")]
        [InlineData(".NET Native 1.0.0", ".NET Native")]
        [InlineData("Mono 5.18.1.0", "Mono")]
        public void GetWorkerMetadata_ParsesFrameworkDescription(string frameworkDescription, string expectedFramework)
        {
            var workerMetadata = GrpcWorker.GetWorkerMetadata(frameworkDescription);
            Assert.Equal(expectedFramework, workerMetadata.RuntimeName);
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
            using var testVariables = new TestScopedEnvironmentVariable("FUNCTIONS_WORKER_DIRECTORY", "test");

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
            using var testVariables = new TestScopedEnvironmentVariable("FUNCTIONS_WORKER_DIRECTORY", "test");

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
        [InlineData("EnableUserCodeException", true, "EnableUserCodeException", true, "True")]
        [InlineData("EnableUserCodeException", false, "EnableUserCodeException", false)]
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
        public void WorkerOptions_CanChangeOptionalCapabilities()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults((WorkerOptions options) =>
                {
                    options.Capabilities.Remove("HandlesWorkerTerminateMessage");
                    options.Capabilities.Add("SomeNewCapability", bool.TrueString);
                }).Build();

            var workerOptions = host.Services.GetService<IOptions<WorkerOptions>>().Value;
            var response = GrpcWorker.WorkerInitRequestHandler(new(), workerOptions);

            void AssertKeyAndValue(KeyValuePair<string, string> kvp, string expectedKey, string expectedValue)
            {
                Assert.Same(expectedKey, kvp.Key);
                Assert.Same(expectedValue, kvp.Value);
            }

            Assert.Collection(response.Capabilities.OrderBy(p => p.Key),
                c => AssertKeyAndValue(c, "EnableUserCodeException", bool.TrueString),
                c => AssertKeyAndValue(c, "HandlesInvocationCancelMessage", bool.TrueString),
                c => AssertKeyAndValue(c, "IncludeEmptyEntriesInMessagePayload", bool.TrueString),
                c => AssertKeyAndValue(c, "RawHttpBodyBytes", bool.TrueString),
                c => AssertKeyAndValue(c, "RpcHttpBodyOnly", bool.TrueString),
                c => AssertKeyAndValue(c, "RpcHttpTriggerMetadataRemoved", bool.TrueString),
                c => AssertKeyAndValue(c, "SomeNewCapability", bool.TrueString),
                c => AssertKeyAndValue(c, "TypedDataCollection", bool.TrueString),
                c => AssertKeyAndValue(c, "UseNullableValueDictionaryForHttp", bool.TrueString),
                c => AssertKeyAndValue(c, "WorkerStatus", bool.TrueString));
        }

        [Fact]
        public void EnvironmentReloadRequestHandler_ReturnsExpected()
        {
            var actual = GrpcWorker.EnvironmentReloadRequestHandler(new WorkerOptions()); ;

            Assert.Equal(StatusResult.Success, actual.Result);
            Assert.NotNull(actual.WorkerMetadata);
            Assert.NotEmpty(actual.Capabilities);
        }

        [Fact]
        public async Task Invocation_WhenSynchronous_DoesNotBlock()
        {
            using var testVariables = new TestScopedEnvironmentVariable("FUNCTIONS_WORKER_DIRECTORY", "test");

            var blockingFunctionEvent = new ManualResetEventSlim();
            var releaseFunctionEvent = new ManualResetEventSlim();

            var clientFactoryMock = new Mock<IWorkerClientFactory>();
            var clientMock = new Mock<IWorkerClient>();
            var metadataProvider = new Mock<IFunctionMetadataProvider>();
            var invocationHandlerMock = new Mock<IInvocationHandler>();

            InvocationResponse ValueFunction(InvocationRequest request)
            {
                if (string.Equals(request.FunctionId, "a"))
                {
                    blockingFunctionEvent.Set();
                    releaseFunctionEvent.Wait();
                }
                else
                {
                    releaseFunctionEvent.Set();
                }

                return new InvocationResponse();
            }

            invocationHandlerMock.Setup(h => h.InvokeAsync(It.IsAny<InvocationRequest>()))
              .ReturnsAsync<InvocationRequest, IInvocationHandler, InvocationResponse>(ValueFunction);

            clientMock.Setup(c => c.SendMessageAsync(It.IsAny<StreamingMessage>()))
                .Returns(ValueTask.CompletedTask);

            clientFactoryMock.Setup(f=>f.CreateClient(It.IsAny<IMessageProcessor>()))
                .Returns(clientMock.Object);

            var worker = new GrpcWorker(_mockApplication.Object,
                                        clientFactoryMock.Object,
                                        _mockMethodInfoLocator.Object,
                                        new OptionsWrapper<WorkerOptions>(new WorkerOptions()),
                                        metadataProvider.Object,
                                        new ApplicationLifetime(TestLogger<ApplicationLifetime>.Create()),
                                        invocationHandlerMock.Object);

            await worker.StartAsync(CancellationToken.None);


            void ProcessMessage(IMessageProcessor processor, string functionId = null)
            {
                processor.ProcessMessageAsync(new StreamingMessage
                {
                    InvocationRequest = new InvocationRequest { FunctionId = functionId }
                });
            }

            _ = Task.Run(() =>
            {
                ProcessMessage(worker, "a");

                // Ensure we're executing the blocking function before invoking
                // the release function
                if (!blockingFunctionEvent.Wait(5000))
                {
                    Assert.Fail("Blocking function event was not set.");
                }

                ProcessMessage(worker, "b");
            });

            releaseFunctionEvent.Wait(5000);

            Assert.True(releaseFunctionEvent.IsSet,
                "Release function was never called. " +
                "This indicates the blocking function prevented execution flow.");
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
