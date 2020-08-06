﻿using System;
using Microsoft.Azure.Functions.DotNetWorker.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.DotNetWorker.Configuration
{
    internal class DotNetApplicationBuilder : IDotNetApplicationBuilder
    {
        private readonly IInvocationPipelineBuilder<FunctionExecutionContext> _pipelineBuilder;

        public IServiceCollection Services { get; private set; }

        public DotNetApplicationBuilder(IServiceCollection services)
        {
            Services = services;
            _pipelineBuilder = new DefaultInvocationPipelineBuilder<FunctionExecutionContext>();
            Services.AddSingleton<FunctionExecutionDelegate>(sp =>
            {
                return _pipelineBuilder.Build();
            });
        }

        public IDotNetApplicationBuilder Use(Func<FunctionExecutionDelegate, FunctionExecutionDelegate> middleware)
        {
            _pipelineBuilder.Use(middleware);
            return this;
        }
    }
}
