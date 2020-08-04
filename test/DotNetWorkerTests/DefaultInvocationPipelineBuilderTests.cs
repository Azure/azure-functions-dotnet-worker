using Microsoft.Azure.Functions.DotNetWorker;
using Microsoft.Azure.Functions.DotNetWorker.Pipeline;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.Functions.DotNetWorkerTests
{
    public class DefaultInvocationPipelineBuilderTests
    {
        Mock<IServiceScopeFactory> _mockServiceScopeFactory;
        InvocationRequest _invocationRequest;

        public DefaultInvocationPipelineBuilderTests()
        {
            _invocationRequest = new InvocationRequest();
            _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        }

        [Fact]
        public void Build_BuildsInCorrectOrder()
        {
            var builder = new DefaultInvocationPipelineBuilder<FunctionExecutionContext>();
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

            var context = new FunctionExecutionContext(_mockServiceScopeFactory.Object, _invocationRequest);

            pipeline(context);

            Assert.Equal(new[] { "Middleware1", "Middleware2", "Middleware3" }, context.Items.Keys);
        }

        [Fact]
        public void Middleware_ShortCircuitsPipeline()
        {
            var builder = new DefaultInvocationPipelineBuilder<FunctionExecutionContext>();
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

            var context = new FunctionExecutionContext(_mockServiceScopeFactory.Object, _invocationRequest);

            pipeline(context);

            Assert.Equal(new[] { "Middleware1", "Middleware2" }, context.Items.Keys);
        }

    }
}
