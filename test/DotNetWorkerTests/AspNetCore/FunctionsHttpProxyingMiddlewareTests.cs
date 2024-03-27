// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;
using Microsoft.Azure.Functions.Worker.Middleware;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.AspNetCore
{
    public class FunctionsHttpProxyingMiddlewareTests
    {
        [Fact]
        public async Task Middleware_AddsHttpContextToFunctionContext_Success()
        {
            var test = SetupInputs("httpTrigger");
            var mockDelegate = new Mock<FunctionExecutionDelegate>();

            var funcMiddleware = new FunctionsHttpProxyingMiddleware(test.MockCoordinator.Object);
            await funcMiddleware.Invoke(test.FunctionContext, mockDelegate.Object);

            Assert.NotNull(test.FunctionContext.GetHttpContext());
            Assert.Equal(test.FunctionContext.GetHttpContext(), test.HttpContext);

            mockDelegate.Verify(p => p.Invoke(test.FunctionContext), Times.Once());

            test.MockCoordinator.Verify(p => p.SetFunctionContextAsync(It.IsAny<string>(), It.IsAny<FunctionContext>()), Times.Once());
            test.MockCoordinator.Verify(p => p.CompleteFunctionInvocation(It.IsAny<string>()), Times.Once());
        }

        [Fact]
        public async Task Middleware_NoOpsOnNonHttpTriggers()
        {
            var test = SetupInputs("someTrigger");
            var mockDelegate = new Mock<FunctionExecutionDelegate>();

            var funcMiddleware = new FunctionsHttpProxyingMiddleware(test.MockCoordinator.Object);
            await funcMiddleware.Invoke(test.FunctionContext, mockDelegate.Object);

            mockDelegate.Verify(p => p.Invoke(test.FunctionContext), Times.Once());

            test.MockCoordinator.Verify(p => p.SetFunctionContextAsync(It.IsAny<string>(), It.IsAny<FunctionContext>()), Times.Never());
            test.MockCoordinator.Verify(p => p.CompleteFunctionInvocation(It.IsAny<string>()), Times.Never());
        }

        private static (FunctionContext FunctionContext, HttpContext HttpContext, Mock<IHttpCoordinator> MockCoordinator) SetupInputs(string triggerType)
        {
            var inputBindings = new Dictionary<string, BindingMetadata>()
            {
                { "test", new TestBindingMetadata("test", triggerType, BindingDirection.In ) }
            };

            var outputBindings = new Dictionary<string, BindingMetadata>()
            {
                { "outputBinding", new TestBindingMetadata("$return", "http", BindingDirection.Out ) }
            };

            var functionDef = new TestFunctionDefinition(inputBindings: inputBindings, outputBindings: outputBindings);

            var functionContext = new TestFunctionContext(functionDef, new TestFunctionInvocation(), CancellationToken.None)
            {
                Items = new Dictionary<object, object>()
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Add(Constants.CorrelationHeader, functionContext.InvocationId);

            var mockCoordinator = new Mock<IHttpCoordinator>();
            mockCoordinator
                .Setup(p => p.SetHttpContextAsync(functionContext.InvocationId, httpContext))
                .ReturnsAsync(functionContext);
            mockCoordinator
                .Setup(p => p.SetFunctionContextAsync(functionContext.InvocationId, functionContext))
                .ReturnsAsync(httpContext);

            return new(functionContext, httpContext, mockCoordinator);
        }
    }
}
