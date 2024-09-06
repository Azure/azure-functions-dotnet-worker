// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.AspNetCore
{
    public class WorkerRequestServicesMiddlewareTests
    {
        [Fact]
        public async Task ServiceProviders_Equal()
        {
            var services = new ServiceCollection();
            services.AddScoped<MyService>();

            // FunctionContext services will be scoped.
            var provider = services.BuildServiceProvider().CreateScope().ServiceProvider;
            var functionService = provider.GetService<MyService>();

            RequestDelegate next = (ctxt) =>
            {
                // Test the service here. Because it is scoped,
                // it should match the one from the FunctionContext.
                var httpService = ctxt.RequestServices.GetService<MyService>();
                Assert.Same(httpService, functionService);
                Assert.Same(provider, ctxt.RequestServices);

                return Task.CompletedTask;
            };

            var functionContext = new TestFunctionContext(new TestFunctionDefinition(), new TestFunctionInvocation(), CancellationToken.None,
                serviceProvider: provider);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Add(Constants.CorrelationHeader, functionContext.InvocationId);

            var httpCoordinator = new Mock<IHttpCoordinator>();
            httpCoordinator
                .Setup(p => p.SetHttpContextAsync(functionContext.InvocationId, httpContext))
                .ReturnsAsync(functionContext);

            var middleware = new WorkerRequestServicesMiddleware(next, httpCoordinator.Object);
            await middleware.Invoke(httpContext);
        }
        
        [Fact]
        public async Task DefaultServiceProvider_Restored()
        {
            var services = new ServiceCollection();
            services.AddScoped<MyService>();
            
            var httpContextProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;

            // FunctionContext services will be scoped.
            var functionContextProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;

            RequestDelegate next = (ctxt) =>
            {
                Assert.Same(functionContextProvider, ctxt.RequestServices);

                return Task.CompletedTask;
            };

            var functionContext = new TestFunctionContext(new TestFunctionDefinition(), new TestFunctionInvocation(), CancellationToken.None,
                serviceProvider: functionContextProvider);

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = httpContextProvider;
            httpContext.Request.Headers.Add(Constants.CorrelationHeader, functionContext.InvocationId);

            var httpCoordinator = new Mock<IHttpCoordinator>();
            httpCoordinator
                .Setup(p => p.SetHttpContextAsync(functionContext.InvocationId, httpContext))
                .ReturnsAsync(functionContext);

            var middleware = new WorkerRequestServicesMiddleware(next, httpCoordinator.Object);
            await middleware.Invoke(httpContext);
            
            Assert.Same(httpContextProvider, httpContext.RequestServices);
            Assert.NotSame(functionContextProvider, httpContext.RequestServices);
        }

        private class MyService;
    }
}

