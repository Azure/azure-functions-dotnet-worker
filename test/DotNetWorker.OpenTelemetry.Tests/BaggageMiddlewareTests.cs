// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Middleware;
using Moq;
using OpenTelemetry;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.OpenTelemetry.Tests
{
    public class BaggageMiddlewareTests
    {
        [Fact]
        public async Task Invoke_SetsAndClearsBaggage_WhenBaggagePresent()
        {
            var baggageKey = "TestKey";
            var baggageValue = "TestValue";
            var baggageDict = new Dictionary<string, string>
            {
                { baggageKey, baggageValue }
            };

            var traceContextMock = new Mock<TraceContext>();
            traceContextMock.Setup(t => t.Baggage).Returns(baggageDict);

            var contextMock = new Mock<FunctionContext>();
            contextMock.Setup(c => c.TraceContext).Returns(traceContextMock.Object);

            bool nextCalled = false;
            var middleware = new BaggageMiddleware();

            // baggage is set before next is called
            FunctionExecutionDelegate next = async ctx =>
            {
                nextCalled = true;
                Assert.Equal(baggageValue, Baggage.GetBaggage(baggageKey));
                await Task.CompletedTask;
            };

            await middleware.Invoke(contextMock.Object, next);

            // cleared after invoking
            Assert.True(nextCalled);
            Assert.Null(Baggage.GetBaggage(baggageKey));
        }

        [Fact]
        public async Task Invoke_ClearsBaggage_OnException()
        {
            var baggageKey = "TestKey";
            var baggageValue = "TestValue";
            var baggageDict = new Dictionary<string, string>
            {
                { baggageKey, baggageValue }
            };

            var traceContextMock = new Mock<TraceContext>();
            traceContextMock.Setup(t => t.Baggage).Returns(baggageDict);

            var contextMock = new Mock<FunctionContext>();
            contextMock.Setup(c => c.TraceContext).Returns(traceContextMock.Object);

            var middleware = new BaggageMiddleware();

            FunctionExecutionDelegate next = ctx =>
            {
                Assert.Equal(baggageValue, Baggage.GetBaggage(baggageKey));
                throw new System.Exception("Test exception");
            };

            await Assert.ThrowsAsync<System.Exception>(() => middleware.Invoke(contextMock.Object, next));
            Assert.Null(Baggage.GetBaggage(baggageKey));
        }
    }

}
