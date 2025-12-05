// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.AspNetMiddleware;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Worker.Extensions.Http.AspNetCore.Tests
{
    public class FunctionsHostBuilderExtensionsTests
    {
        [Fact]
        public void ConfigureFunctionsWebApplication_ShouldConfigureFunctionsWebApplicationWithoutAction()
        {
            var underTest = new HostBuilder();
            underTest.ConfigureFunctionsWebApplication();
            var host = underTest.Build();
            VerifyRegistrationOfAspNetCoreIntegrationServices(host);
        }

        [Fact]
        public void ConfigureFunctionsWebApplication_ShouldConfigureFunctionsWebApplicationWithActionForBuilder()
        {
            var underTest = new HostBuilder();
            underTest.ConfigureFunctionsWebApplication(builder => builder.UseMiddleware<TestMiddleware>());
            var host = underTest.Build();
            VerifyRegistrationOfCustomMiddleware(host);
            VerifyRegistrationOfAspNetCoreIntegrationServices(host);
        }

        [Fact]
        public void ConfigureFunctionsWebApplication_ShouldConfigureFunctionsWebApplicationWithActionForContext()
        {
            const string expectedConfigValue = "test_config_value";
            const string configKey = "test_config_key";

            var underTest = new HostBuilder();
            underTest.ConfigureHostConfiguration(builder =>
            {
                builder.Add(new MemoryConfigurationSource { InitialData = new Dictionary<string, string?> { { configKey, expectedConfigValue } } });
            });
            string? actualConfigValue = null;

            underTest.ConfigureFunctionsWebApplication((context, builder) =>
            {
                actualConfigValue = context.Configuration.GetValue<string>(configKey);
                builder.UseMiddleware<TestMiddleware>();
            });
            var host = underTest.Build();

            Assert.Equal(expectedConfigValue, actualConfigValue);
            VerifyRegistrationOfCustomMiddleware(host);
            VerifyRegistrationOfAspNetCoreIntegrationServices(host);
        }

        [Fact]
        public void ConfigureFunctionsWebApplication_CalledTwice_ShouldBeIdempotent()
        {
            var builder = new HostBuilder();

            int callbackCount = 0;

            // Call ConfigureFunctionsWebApplication twice but should call callbacks each time
            builder.ConfigureFunctionsWebApplication((_, _) => callbackCount++);
            builder.ConfigureFunctionsWebApplication((_, _) => callbackCount++);

            IServiceCollection serviceCollection = default!;
            builder.ConfigureServices(services =>
            {
                serviceCollection = services;
            });

            var host = builder.Build();

            // Verify services are registered
            VerifyRegistrationOfAspNetCoreIntegrationServices(host);

            Assert.Equal(2, callbackCount);

            ValidateSingleServiceType(serviceCollection, typeof(FunctionsHttpProxyingMiddleware));
            ValidateSingleServiceType(serviceCollection, typeof(IHttpCoordinator));
            ValidateSingleServiceType(serviceCollection, typeof(FunctionsEndpointDataSource));

            static void ValidateSingleServiceType(IServiceCollection services, Type type)
            {
                Assert.NotNull(services);
                var coordinatorDescriptors = services
                    .Where(d => d.ServiceType == type)
                    .ToList();
                Assert.Single(coordinatorDescriptors);
            }
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
                return Task.CompletedTask;
            }
        }
    }
}
