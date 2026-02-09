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
            var baggageDict = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(baggageKey, baggageValue)
            };

            var contextMock = new Mock<FunctionContext>();
            var items = new Dictionary<object, object>
            {
                { TraceConstants.BaggageKeyName, baggageDict }
            };
            contextMock.Setup(c => c.Items).Returns(items);

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
            var baggageDict = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>(baggageKey, baggageValue)
        };

            var contextMock = new Mock<FunctionContext>();
            var items = new Dictionary<object, object>
        {
            { TraceConstants.BaggageKeyName, baggageDict }
        };
            contextMock.Setup(c => c.Items).Returns(items);

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
