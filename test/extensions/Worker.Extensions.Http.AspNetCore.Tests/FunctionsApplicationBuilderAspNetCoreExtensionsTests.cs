// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.AspNetMiddleware;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Worker.Extensions.Http.AspNetCore.Tests
{
    public class FunctionsApplicationBuilderAspNetCoreExtensionsTests
    {
        [Fact]
        public void ConfigureFunctionsWebApplication_WithAction_RegistersMiddlewareAndAspNetCoreServices()
        {
            var builder = FunctionsApplication.CreateBuilder([]);
            builder.ConfigureFunctionsWebApplication(b => b.UseMiddleware<TestMiddleware>());
            var host = builder.Build();

            VerifyRegistrationOfCustomMiddleware(host);
            VerifyRegistrationOfAspNetCoreIntegrationServices(host);
        }

        [Fact]
        public void ConfigureFunctionsWebApplication_WithContextAction_ReceivesContextAndRegistersAspNetCoreServices()
        {
            HostBuilderContext? capturedContext = null;

            var builder = FunctionsApplication.CreateBuilder([]);
            builder.ConfigureFunctionsWebApplication((ctx, _) => capturedContext = ctx);
            var host = builder.Build();

            Assert.NotNull(capturedContext);
            VerifyRegistrationOfAspNetCoreIntegrationServices(host);
        }

        [Fact]
        public void ConfigureFunctionsWebApplication_WithNullAction_ThrowsArgumentNullException()
        {
            var builder = FunctionsApplication.CreateBuilder([]);
            Assert.Throws<ArgumentNullException>("configureWorker",
                () => builder.ConfigureFunctionsWebApplication((Action<IFunctionsWorkerApplicationBuilder>)null!));
        }

        [Fact]
        public void ConfigureFunctionsWebApplication_WithNullContextAction_ThrowsArgumentNullException()
        {
            var builder = FunctionsApplication.CreateBuilder([]);
            Assert.Throws<ArgumentNullException>("configureWorker",
                () => builder.ConfigureFunctionsWebApplication((Action<HostBuilderContext, IFunctionsWorkerApplicationBuilder>)null!));
        }

        [Fact]
        public void ConfigureFunctionsWebApplication_WithActionCalledTwice_AspNetCoreServicesRegisteredOnce()
        {
            var callbackCount = 0;
            var builder = FunctionsApplication.CreateBuilder([]);
            builder.ConfigureFunctionsWebApplication(_ => { callbackCount++; });
            builder.ConfigureFunctionsWebApplication(_ => { callbackCount++; });

            // Check descriptor counts before Build() to catch duplicate registrations
            var endpointDataSources = builder.Services.Where(d => d.ServiceType == typeof(FunctionsEndpointDataSource)).ToList();
            var httpProxyingMiddlewares = builder.Services.Where(d => d.ServiceType == typeof(FunctionsHttpProxyingMiddleware)).ToList();
            var coordinators = builder.Services.Where(d => d.ServiceType == typeof(IHttpCoordinator)).ToList();
            Assert.Single(endpointDataSources);
            Assert.Single(httpProxyingMiddlewares);
            Assert.Single(coordinators);

            // Also verify end-to-end resolution yields single instances
            var host = builder.Build();
            Assert.Single(host.Services.GetServices<FunctionsEndpointDataSource>());
            Assert.Single(host.Services.GetServices<FunctionsHttpProxyingMiddleware>());
            Assert.Single(host.Services.GetServices<IHttpCoordinator>());
            Assert.Equal(2, callbackCount);
        }

        private static void VerifyRegistrationOfCustomMiddleware(IHost host)
        {
            Assert.NotNull(host.Services.GetService<TestMiddleware>());
        }

        private static void VerifyRegistrationOfAspNetCoreIntegrationServices(IHost host)
        {
            Assert.NotNull(host.Services.GetService<FunctionsEndpointDataSource>());
            Assert.NotNull(host.Services.GetService<FunctionsHttpProxyingMiddleware>());
            var httpCoordinator = host.Services.GetService<IHttpCoordinator>();
            Assert.NotNull(httpCoordinator);
            Assert.IsType<DefaultHttpCoordinator>(httpCoordinator);
        }

        private class TestMiddleware : IFunctionsWorkerMiddleware
        {
            public Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
            {
                return next(context);
            }
        }
    }
}
