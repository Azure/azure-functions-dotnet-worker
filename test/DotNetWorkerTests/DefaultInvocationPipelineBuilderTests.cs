// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class DefaultInvocationPipelineBuilderTests
    {
        private Mock<FunctionContext> _mockContext = new Mock<FunctionContext>();

        public DefaultInvocationPipelineBuilderTests()
        {
            _mockContext = new Mock<FunctionContext>();
            _mockContext.SetupAllProperties();
        }

        [Fact]
        public void Build_BuildsInCorrectOrder()
        {
            var builder = new DefaultInvocationPipelineBuilder<FunctionContext>();
            builder.Use(next => context =>
            {
                context.Items.Add("Middleware1", null);
                return next(context);
            });

            builder.Use(next => context =>
            {
                context.Items.Add("Middleware2", null);
                return next(context);
            });

            builder.Use(next => context =>
            {
                context.Items.Add("Middleware3", null);
                return next(context);
            });

            var pipeline = builder.Build();

            var context = _mockContext.Object;
            context.Items = new Dictionary<object, object>();

            pipeline(context);

            Assert.Equal(new[] { "Middleware1", "Middleware2", "Middleware3" }, context.Items.Keys);
        }

        [Fact]
        public void Middleware_ShortCircuitsPipeline()
        {
            var builder = new DefaultInvocationPipelineBuilder<FunctionContext>();
            builder.Use(next => context =>
            {
                context.Items.Add("Middleware1", null);
                return next(context);
            });

            builder.Use(next => context =>
            {
                context.Items.Add("Middleware2", null);

                return Task.CompletedTask;
            });

            builder.Use(next => context =>
            {
                context.Items.Add("Middleware3", null);
                return next(context);
            });

            var pipeline = builder.Build();

            var context = _mockContext.Object;
            context.Items = new Dictionary<object, object>();

            pipeline(context);

            Assert.Equal(new[] { "Middleware1", "Middleware2" }, context.Items.Keys);
        }

        [Fact]
        public void InlineMiddleware_RunsInExpectedOrder()
        {
            var services = new ServiceCollection();
            IFunctionsWorkerApplicationBuilder builder = new FunctionsWorkerApplicationBuilder(services);

            builder.Use(next => context =>
            {
                context.Items.Add("Middleware1", null);
                return next(context);
            });

            builder.UseMiddleware((context, next) =>
            {
                context.Items.Add("Middleware2", null);
                return next();
            });

            builder.Use(next => context =>
            {
                context.Items.Add("Middleware3", null);
                return next(context);
            });

            FunctionExecutionDelegate app = builder.Services
                .BuildServiceProvider()
                .GetService<FunctionExecutionDelegate>();

            var context = _mockContext.Object;
            context.Items = new Dictionary<object, object>();

            app(context);

            Assert.Equal(new[] { "Middleware1", "Middleware2", "Middleware3" }, context.Items.Keys);
        }

    }
}
