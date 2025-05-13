// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Infrastructure;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.AspNetCore
{    public class FunctionsHttpProxyingMiddlewareTests
    {
        [Fact]
        public async Task Middleware_AddsHttpContextToFunctionContext_Success()
        {
            var test = SetupTest("httpTrigger");
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
            var test = SetupTest("someTrigger");
            var mockDelegate = new Mock<FunctionExecutionDelegate>();

            var funcMiddleware = new FunctionsHttpProxyingMiddleware(test.MockCoordinator.Object);
            await funcMiddleware.Invoke(test.FunctionContext, mockDelegate.Object);

            mockDelegate.Verify(p => p.Invoke(test.FunctionContext), Times.Once());

            test.MockCoordinator.Verify(p => p.SetFunctionContextAsync(It.IsAny<string>(), It.IsAny<FunctionContext>()), Times.Never());
            test.MockCoordinator.Verify(p => p.CompleteFunctionInvocation(It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public async Task SimpleHttpTrigger_ActionResultHandled()
        {
            var test = SetupTest("httpTrigger");
            var mockDelegate = new Mock<FunctionExecutionDelegate>();

            // In a simple HTTP trigger function, there is only one input (the trigger), and the HTTP response
            // The HTTP response will be in the InvocationResult
            var mockActionResult = GetMockActionResult();
            var bindingFeatures = test.FunctionContext.Features.Get<IFunctionBindingsFeature>();
            bindingFeatures.InvocationResult = mockActionResult.Object;

            var funcMiddleware = new FunctionsHttpProxyingMiddleware(test.MockCoordinator.Object);
            await funcMiddleware.Invoke(test.FunctionContext, mockDelegate.Object);

            mockActionResult.Verify();
            test.MockCoordinator.Verify(p => p.CompleteFunctionInvocation(It.IsAny<string>()), Times.Once());
        }

        [Fact]
        public async Task SimpleHttpTrigger_IResultHandled()
        {
            var test = SetupTest("httpTrigger");
            var mockDelegate = new Mock<FunctionExecutionDelegate>();

            // In a simple HTTP trigger function, there is only one input (the trigger), and the HTTP response
            // The HTTP response will be in the InvocationResult
            var mockResult = GetMockIResult();
            var bindingFeatures = test.FunctionContext.Features.Get<IFunctionBindingsFeature>();
            bindingFeatures.InvocationResult = mockResult.Object;

            var funcMiddleware = new FunctionsHttpProxyingMiddleware(test.MockCoordinator.Object);
            await funcMiddleware.Invoke(test.FunctionContext, mockDelegate.Object);

            mockResult.Verify();
            test.MockCoordinator.Verify(p => p.CompleteFunctionInvocation(It.IsAny<string>()), Times.Once());
        }

        [Fact]
        public async Task MultipleOutput_IActionResultIsHandled()
        {
            var test = SetupTest("httpTrigger", GetMultiOutputTypeOutputBindings());
            var mockDelegate = new Mock<FunctionExecutionDelegate>();

            // In a multi-output HTTP trigger function, the HTTP response is stored in the output bindings, and InvocationResult is empty
            var mockActionResult = GetMockActionResult();
            var bindingFeatures = test.FunctionContext.Features.Get<IFunctionBindingsFeature>();
            bindingFeatures.OutputBindingData.Add("result", mockActionResult.Object);

            var funcMiddleware = new FunctionsHttpProxyingMiddleware(test.MockCoordinator.Object);
            await funcMiddleware.Invoke(test.FunctionContext, mockDelegate.Object);

            mockActionResult.Verify();
        }

        [Fact]
        public async Task MultipleOutput_IResultIsHandled()
        {
            var test = SetupTest("httpTrigger", GetMultiOutputTypeOutputBindings());
            var mockDelegate = new Mock<FunctionExecutionDelegate>();

            // In a multi-output HTTP trigger function, the HTTP response is stored in the output bindings, and InvocationResult is empty
            var mockResult = GetMockIResult();
            var bindingFeatures = test.FunctionContext.Features.Get<IFunctionBindingsFeature>();
            bindingFeatures.OutputBindingData.Add("result", mockResult.Object);

            var funcMiddleware = new FunctionsHttpProxyingMiddleware(test.MockCoordinator.Object);
            await funcMiddleware.Invoke(test.FunctionContext, mockDelegate.Object);

            mockResult.Verify();
        }

        [Fact]
        public async Task Multiple_OutputAspNetHttpRequestData_Completes()
        {
            var test = SetupTest("httpTrigger", GetMultiOutputTypeOutputBindings());
            var mockDelegate = new Mock<FunctionExecutionDelegate>();

            SetUpAspNetCoreHttpResponseDataBindingInfo(test.FunctionContext, false);

            var funcMiddleware = new FunctionsHttpProxyingMiddleware(test.MockCoordinator.Object);
            await funcMiddleware.Invoke(test.FunctionContext, mockDelegate.Object);

            test.MockCoordinator.Verify(p => p.CompleteFunctionInvocation(It.IsAny<string>()), Times.Once());
        }

        [Fact]
        public async Task InvocationResultNullInMultiOutput_WhenResultIsTypeAspNetCoreHttpResponseData()
        {
            var test = SetupTest("httpTrigger", GetMultiOutputTypeOutputBindings());
            var mockDelegate = new Mock<FunctionExecutionDelegate>();

            SetUpAspNetCoreHttpResponseDataBindingInfo(test.FunctionContext, true);

            var funcMiddleware = new FunctionsHttpProxyingMiddleware(test.MockCoordinator.Object);
            await funcMiddleware.Invoke(test.FunctionContext, mockDelegate.Object);

            Assert.Null(test.FunctionContext.GetInvocationResult().Value);
            test.MockCoordinator.Verify(p => p.CompleteFunctionInvocation(It.IsAny<string>()), Times.Once());
        }

        [Fact]
        public async Task InvocationResultNull_WhenResultIsTypeAspNetCoreHttpResponseData()
        {
            var test = SetupTest("httpTrigger");
            var mockDelegate = new Mock<FunctionExecutionDelegate>();

            SetUpAspNetCoreHttpResponseDataBindingInfo(test.FunctionContext, false);

            var funcMiddleware = new FunctionsHttpProxyingMiddleware(test.MockCoordinator.Object);
            await funcMiddleware.Invoke(test.FunctionContext, mockDelegate.Object);

            Assert.Null(test.FunctionContext.GetInvocationResult().Value);
            test.MockCoordinator.Verify(p => p.CompleteFunctionInvocation(It.IsAny<string>()), Times.Once());
        }

        private static (FunctionContext FunctionContext, HttpContext HttpContext, Mock<IHttpCoordinator> MockCoordinator) SetupTest(string triggerType, IDictionary<string, BindingMetadata> outputBindings = null)
        {
            var inputBindings = new Dictionary<string, BindingMetadata>()
            {
                { "test", new TestBindingMetadata("test", triggerType, BindingDirection.In ) }
            };

            var functionDefinition = new TestFunctionDefinition(inputBindings: inputBindings, outputBindings: outputBindings);

            var serviceProvider = new ServiceCollection()
                .AddSingleton<ExtensionTrace>()
                .AddLogging()
                .BuildServiceProvider();

            var functionContext = new TestFunctionContext(functionDefinition, new TestFunctionInvocation(), CancellationToken.None)
            {
                Items = new Dictionary<object, object>(),
                InstanceServices = serviceProvider
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Append(Constants.CorrelationHeader, functionContext.InvocationId);

            var mockCoordinator = new Mock<IHttpCoordinator>();
            mockCoordinator
                .Setup(p => p.SetHttpContextAsync(functionContext.InvocationId, httpContext))
                .ReturnsAsync(functionContext);
            mockCoordinator
                .Setup(p => p.SetFunctionContextAsync(functionContext.InvocationId, functionContext))
                .ReturnsAsync(httpContext);

            return new(functionContext, httpContext, mockCoordinator);
        }

        /// <summary>
        /// A dictionary of binding metadata representing the output bindings in a Multi-Ouptut HTTP trigger function 
        /// with a POCO that has a queue output and an http result.
        /// </summary>
        /// <returns>Dictionary with entries of (name, bindingMetadata).</returns>
        private Dictionary<string, BindingMetadata> GetMultiOutputTypeOutputBindings()
        {
            var outputBindings = new Dictionary<string, BindingMetadata>()
            {
                { "name", new TestBindingMetadata("name", "queue", BindingDirection.Out) },
                { "result", new TestBindingMetadata("result", "http", BindingDirection.Out)}
            };

            return outputBindings;
        }

        /// <summary>
        /// Sets up an AspNetCoreHttpResponseData object using a mock HTTP response and stores it in the appropriate location in a FunctionContext instance.
        /// </summary>
        /// <param name="functionContext">The function context used for testing.</param>
        /// <param name="isInvocationResult">True if the object is expected to be in the invocation result, false if the object is expected in the output bindings (which is the case for
        /// multi-output scenarios).</param>
        private void SetUpAspNetCoreHttpResponseDataBindingInfo(FunctionContext functionContext, bool isInvocationResult)
        {
            var mockResponse = new Mock<HttpResponse>();

            var bindingFeatures = functionContext.Features.Get<IFunctionBindingsFeature>();
            var aspNetCoreHttpResponseData = new AspNetCoreHttpResponseData(mockResponse.Object, functionContext);

            if (isInvocationResult)
            {
                bindingFeatures.InvocationResult = aspNetCoreHttpResponseData;
            }
            else
            {
                bindingFeatures.OutputBindingData.Add("result", aspNetCoreHttpResponseData);
            }
        }

        private Mock<IActionResult> GetMockActionResult()
        {
            var mockActionResult = new Mock<IActionResult>();
            mockActionResult.Setup(p => p.ExecuteResultAsync(It.IsAny<ActionContext>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            return mockActionResult;
        }

        private Mock<IResult> GetMockIResult()
        {
            var mockResult = new Mock<IResult>();
            mockResult.Setup(p => p.ExecuteAsync(It.IsAny<HttpContext>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            return mockResult;
        }

        [Fact]
        public async Task CompleteFunctionInvocation_RunsWhen_FunctionThrowsException()
        {
            var test = SetupTest("httpTrigger");
            var mockDelegate = new Mock<FunctionExecutionDelegate>();
            mockDelegate.Setup(d => d.Invoke(It.IsAny<FunctionContext>()))
                .Throws(new Exception("Custom exception message"));

            var funcMiddleware = new FunctionsHttpProxyingMiddleware(test.MockCoordinator.Object);

            await Assert.ThrowsAsync<Exception>(async () => await funcMiddleware.Invoke(test.FunctionContext, mockDelegate.Object));

            mockDelegate.Verify(p => p.Invoke(test.FunctionContext), Times.Once());
            test.MockCoordinator.Verify(p => p.CompleteFunctionInvocation(It.IsAny<string>()), Times.Once());
        }
    }
}
