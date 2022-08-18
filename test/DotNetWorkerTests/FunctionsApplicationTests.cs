// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class FunctionsApplicationTests
    {
        private readonly Mock<FunctionExecutionDelegate> _mockFunctionExecutionDelegate = new(MockBehavior.Strict);
        private readonly Mock<IFunctionContextFactory> _mockFunctionContextFactory = new(MockBehavior.Strict);
        private readonly Mock<IOptions<WorkerOptions>> _mockWorkerOptions = new(MockBehavior.Strict);
        private readonly Mock<IWorkerDiagnostics> _mockWorkerDiagnostics = new(MockBehavior.Strict);
        private readonly FunctionInvocationManager _functionInvocationManager = new();
        private readonly TestLoggerProvider _testLoggerProvider = new();
        private readonly ILogger<FunctionsApplication> _testLogger;

        public FunctionsApplicationTests()
        {
            _mockWorkerDiagnostics
                .Setup(m => m.OnFunctionLoaded(It.IsAny<FunctionDefinition>()));

            _mockFunctionContextFactory
                .Setup(m => m.Create(It.IsAny<IInvocationFeatures>(), It.IsAny<CancellationToken>()))
                .Returns((IInvocationFeatures f, CancellationToken ct) => { return new TestFunctionContext(f, ct); });

            LoggerFactory testLoggerFactory = new LoggerFactory(new[] { _testLoggerProvider });
            _testLogger = testLoggerFactory.CreateLogger<FunctionsApplication>();
        }


        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void CreateContext_WithBindsToCancellationToken_CreatesExpectedCancellationToken(bool tokenBinding, bool tokenCancelable)
        {
            var functionId = "abc";
            var invocationId = "5fb3a9b4-0b38-450a-9d46-35946e7edea7";

            var application = new FunctionsApplication(_mockFunctionExecutionDelegate.Object,
                            _mockFunctionContextFactory.Object, _functionInvocationManager,
                            _mockWorkerOptions.Object, _testLogger, _mockWorkerDiagnostics.Object);

            var definition = new TestFunctionDefinition(functionId, bindsToCancellationToken: tokenBinding);
            application.LoadFunction(definition);

            var features = new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>());
            var invocation = new TestFunctionInvocation(invocationId, functionId);
            features.Set<FunctionInvocation>(invocation);

            var context = application.CreateContext(features);

            // If a function binds to a cancellation token, we create a real cancellation token which can be cancelled
            // otherwise we set the token to be  CancellationToken.Empty which cannot be cancelled
            Assert.Equal(tokenCancelable, context.CancellationToken.CanBeCanceled);
        }

        [Fact]
        public void CancelInvocation_CancelsTokenSource()
        {
            var invocationId = "5fb3a9b4-0b38-450a-9d46-35946e7edea7";
            var invocation = new TestFunctionInvocation(invocationId);
            var cts = new CancellationTokenSource();

            var invocationDetails = new FunctionInvocationDetails()
                                    {
                                        FunctionContext = new TestFunctionContext(),
                                        CancellationTokenSource = cts,
                                    };

            _functionInvocationManager.TryAddInvocationDetails(invocationId, invocationDetails);

            var application = new FunctionsApplication(_mockFunctionExecutionDelegate.Object,
                _mockFunctionContextFactory.Object, _functionInvocationManager,
                _mockWorkerOptions.Object, _testLogger, _mockWorkerDiagnostics.Object);

            application.CancelInvocation(invocationId);

            Assert.True(cts.IsCancellationRequested);
            Assert.True(cts.Token.IsCancellationRequested);
        }

        [Fact]
        public void CancelInvocation_InvocationIdNotFound_NoAction()
        {
            var invocationId = "5fb3a9b4-0b38-450a-9d46-35946e7edea7";
            var cts = new CancellationTokenSource();

            var application = new FunctionsApplication(_mockFunctionExecutionDelegate.Object,
                _mockFunctionContextFactory.Object, _functionInvocationManager,
                _mockWorkerOptions.Object, _testLogger, _mockWorkerDiagnostics.Object);

            application.CancelInvocation(invocationId);

            Assert.False(cts.IsCancellationRequested);
            Assert.False(cts.Token.IsCancellationRequested);
        }

        [Fact]
        public void CancelInvocation_CancellationTokenSourceIsNull_NoAction()
        {
            var invocationId = "5fb3a9b4-0b38-450a-9d46-35946e7edea7";
            var invocation = new TestFunctionInvocation(invocationId);

            var invocationDetails = new FunctionInvocationDetails()
                                    {
                                        FunctionContext = new TestFunctionContext(),
                                        CancellationTokenSource = null,
                                    };

            _functionInvocationManager.TryAddInvocationDetails(invocationId, invocationDetails);

            var application = new FunctionsApplication(_mockFunctionExecutionDelegate.Object,
                _mockFunctionContextFactory.Object, _functionInvocationManager,
                _mockWorkerOptions.Object, _testLogger, _mockWorkerDiagnostics.Object);

            application.CancelInvocation(invocationId);
        }

        [Fact]
        public void CancelInvocation_TokenSourceDisposed_Throws()
        {
            var invocationId = "5fb3a9b4-0b38-450a-9d46-35946e7edea7";
            var invocation = new TestFunctionInvocation(invocationId);
            var cts = new CancellationTokenSource();

            var invocationDetails = new FunctionInvocationDetails()
                                    {
                                        FunctionContext = new TestFunctionContext(),
                                        CancellationTokenSource = cts,
                                    };

            _functionInvocationManager.TryAddInvocationDetails(invocationId, invocationDetails);

            var application = new FunctionsApplication(_mockFunctionExecutionDelegate.Object,
                _mockFunctionContextFactory.Object, _functionInvocationManager,
                _mockWorkerOptions.Object, _testLogger, _mockWorkerDiagnostics.Object);

            cts.Dispose();
            application.CancelInvocation(invocationId);

            var log = _testLoggerProvider.GetAllLogMessages().Last().FormattedMessage;
            Assert.Contains("Unable to cancel invocation", log);
        }
    }
}
