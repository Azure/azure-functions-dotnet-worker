// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class FunctionsApplicationTests
    {
        [Fact]
        public async Task InvokeAsync_LogsException()
        {
            static Task InvokeWithError(FunctionContext context)
            {
                throw new InvalidOperationException("boom!");
            }

            var logger = TestLogger<FunctionsApplication>.Create();
            var app = CreateApplication(InvokeWithError, logger);

            var context = new TestFunctionContext();

            await Assert.ThrowsAsync<InvalidOperationException>(() => app.InvokeFunctionAsync(context));

            var message = logger.GetLogMessages().Single();

            var name = message.State.Single(p => p.Key == "functionName").Value;
            Assert.Equal(context.FunctionDefinition.Name, name.ToString());

            var invocation = message.State.Single(p => p.Key == "invocationId").Value;
            Assert.Equal(context.InvocationId, invocation.ToString());

            Assert.Equal(LogLevel.Error, message.Level);
            Assert.IsType<InvalidOperationException>(message.Exception);
            Assert.Equal("boom!", message.Exception.Message);
            Assert.Equal("InvocationError", message.EventId.Name);
            Assert.Equal(2, message.EventId.Id);
        }

        [Fact]
        public async Task InvokeAsync_RecordsActivity()
        {
            var context = new TestFunctionContext();

            using ActivityListener listener = CreateListener(activity =>
            {
                AssertActivity(activity, context);

                Assert.Equal(ActivityStatusCode.Unset, activity.Status);
                Assert.Null(activity.StatusDescription);

                Assert.Empty(activity.Events);
            });

            static Task Invoke(FunctionContext context)
            {
                return Task.CompletedTask;
            }

            var logger = NullLogger<FunctionsApplication>.Instance;
            var app = CreateApplication(Invoke, logger);

            await app.InvokeFunctionAsync(context);
        }

        [Fact]
        public async Task InvokeAsync_OnError_RecordsActivity()
        {
            var context = new TestFunctionContext();

            using ActivityListener listener = CreateListener(activity =>
            {
                AssertActivity(activity, context);

                Assert.Equal(ActivityStatusCode.Error, activity.Status);
                Assert.Equal("boom!", activity.StatusDescription);

                // we are not currently recording exceptions here
                Assert.Empty(activity.Events);
            });

            static Task InvokeWithError(FunctionContext context)
            {
                throw new InvalidOperationException("boom!");
            }

            var logger = NullLogger<FunctionsApplication>.Instance;
            var app = CreateApplication(InvokeWithError, logger);

            await Assert.ThrowsAsync<InvalidOperationException>(() => app.InvokeFunctionAsync(context));
        }

        [Fact]
        public async Task InvokeAsync_SetsBaggageWhenPresent()
        {
            // Arrange
            var baggage = new Dictionary<string, string>
            {
                { "userId", "12345" },
                { "requestId", "abc-def" }
            };
            var context = new TestFunctionContext(baggage: baggage);
            var baggagePropagator = new Mock<IBaggagePropagator>();
            var mockDisposable = new Mock<IDisposable>();
            baggagePropagator.Setup(x => x.SetBaggage(baggage)).Returns(mockDisposable.Object);

            using ActivityListener listener = CreateListener(activity => { });
            using var parentActivity = new Activity("test-parent").Start();

            static Task Invoke(FunctionContext context) => Task.CompletedTask;

            var app = CreateApplicationWithBaggagePropagator(Invoke, baggagePropagator.Object);

            // Act
            await app.InvokeFunctionAsync(context);

            // Assert
            baggagePropagator.Verify(x => x.SetBaggage(baggage), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_DisposesBaggageScopeAfterInvocation()
        {
            // Arrange
            var baggage = new Dictionary<string, string> { { "key", "value" } };
            var context = new TestFunctionContext(baggage: baggage);
            var baggagePropagator = new Mock<IBaggagePropagator>();
            var mockDisposable = new Mock<IDisposable>();
            baggagePropagator.Setup(x => x.SetBaggage(baggage)).Returns(mockDisposable.Object);

            using ActivityListener listener = CreateListener(activity => { });
            using var parentActivity = new Activity("test-parent").Start();

            static Task Invoke(FunctionContext context) => Task.CompletedTask;

            var app = CreateApplicationWithBaggagePropagator(Invoke, baggagePropagator.Object);

            // Act
            await app.InvokeFunctionAsync(context);

            // Assert
            mockDisposable.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_DisposesBaggageScopeOnException()
        {
            // Arrange
            var baggage = new Dictionary<string, string> { { "key", "value" } };
            var context = new TestFunctionContext(baggage: baggage);
            var baggagePropagator = new Mock<IBaggagePropagator>();
            var mockDisposable = new Mock<IDisposable>();
            baggagePropagator.Setup(x => x.SetBaggage(baggage)).Returns(mockDisposable.Object);

            using ActivityListener listener = CreateListener(activity => { });
            using var parentActivity = new Activity("test-parent").Start();

            static Task InvokeWithError(FunctionContext context)
            {
                throw new InvalidOperationException("boom!");
            }

            var app = CreateApplicationWithBaggagePropagator(InvokeWithError, baggagePropagator.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => app.InvokeFunctionAsync(context));
            mockDisposable.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_DoesNotSetBaggageWhenEmpty()
        {
            // Arrange
            var context = new TestFunctionContext(); // No baggage
            var baggagePropagator = new Mock<IBaggagePropagator>();

            static Task Invoke(FunctionContext context) => Task.CompletedTask;

            var app = CreateApplicationWithBaggagePropagator(Invoke, baggagePropagator.Object);

            // Act
            await app.InvokeFunctionAsync(context);

            // Assert
            baggagePropagator.Verify(x => x.SetBaggage(It.IsAny<IEnumerable<KeyValuePair<string, string>>>()), Times.Never);
        }

        private static FunctionsApplication CreateApplicationWithBaggagePropagator(
            FunctionExecutionDelegate invoke,
            IBaggagePropagator baggagePropagator)
        {
            var options = new OptionsWrapper<WorkerOptions>(new WorkerOptions());
            var contextFactory = new Mock<IFunctionContextFactory>();
            var diagnostics = new Mock<IWorkerDiagnostics>();
            var telemetryProvider = new TelemetryProviderV1_17_0();
            var logger = NullLogger<FunctionsApplication>.Instance;

            return new FunctionsApplication(invoke, contextFactory.Object, options, logger, diagnostics.Object, telemetryProvider, baggagePropagator);
        }

        private static void AssertActivity(Activity activity, FunctionContext context)
        {
            Assert.Equal("Invoke", activity.DisplayName);
            Assert.Equal(2, activity.Tags.Count());
            Assert.Equal("https://opentelemetry.io/schemas/1.17.0", activity.Tags.Single(k => k.Key == "az.schema_url").Value);
            Assert.Equal(context.InvocationId, activity.Tags.Single(k => k.Key == "faas.execution").Value);
        }

        private static FunctionsApplication CreateApplication(FunctionExecutionDelegate invoke, ILogger<FunctionsApplication> logger)
        {
            var options = new OptionsWrapper<WorkerOptions>(new WorkerOptions());
            var contextFactory = new Mock<IFunctionContextFactory>();
            var diagnostics = new Mock<IWorkerDiagnostics>();
            var telemetryProvider = new TelemetryProviderV1_17_0();
            var baggagePropagator = new Mock<IBaggagePropagator>();

            return new FunctionsApplication(invoke, contextFactory.Object, options, logger, diagnostics.Object, telemetryProvider, baggagePropagator.Object);
        }

        private static ActivityListener CreateListener(Action<Activity> onStopped)
        {
            var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name.StartsWith(TraceConstants.ActivityAttributes.Name),
                ActivityStarted = activity => { },
                ActivityStopped = onStopped,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
            };

            ActivitySource.AddActivityListener(listener);

            return listener;
        }
    }
}
