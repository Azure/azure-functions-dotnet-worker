// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;
using Microsoft.Azure.Functions.Worker.Middleware;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.AspNetCore
{
    public class FunctionsHttpProxyingMiddlewareTests
    {
        private TestFunctionContext _functionContext;
        private HttpContext _httpContext;
        private Mock<IHttpCoordinator> _mockCoordinator;


        public FunctionsHttpProxyingMiddlewareTests()
        {
            _functionContext = new TestFunctionContext(new TestFunctionDefinition(), new TestFunctionInvocation(), CancellationToken.None);
            _functionContext.Items = new Dictionary<object, object>();

            _httpContext = new DefaultHttpContext();
            _httpContext.Request.Headers.Add(Constants.CorrelationHeader, _functionContext.InvocationId);

            _mockCoordinator = new Mock<IHttpCoordinator>();
            _mockCoordinator
                .Setup(p => p.SetHttpContextAsync(_functionContext.InvocationId, _httpContext))
                .ReturnsAsync(_functionContext);
            _mockCoordinator
                .Setup(p => p.SetFunctionContextAsync(_functionContext.InvocationId, _functionContext))
                .ReturnsAsync(_httpContext);
        }

        [Fact]
        public async void MiddlewareAddsHttpContextToFunctionContext_Sucess()
        {
            var funcMiddleware = new FunctionsHttpProxyingMiddleware(_mockCoordinator.Object);

            var mockDelegate = new Mock<FunctionExecutionDelegate>();

            await funcMiddleware.Invoke(_functionContext, mockDelegate.Object);

            Assert.NotNull(_functionContext.GetHttpContext());
            Assert.Equal(_functionContext.GetHttpContext(), _httpContext);
        }
    }
}
