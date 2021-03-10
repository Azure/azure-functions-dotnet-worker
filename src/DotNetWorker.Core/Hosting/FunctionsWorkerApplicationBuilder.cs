// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Configuration
{
    internal class FunctionsWorkerApplicationBuilder : IFunctionsWorkerApplicationBuilder
    {
        private readonly IInvocationPipelineBuilder<FunctionContext> _pipelineBuilder;

        public IServiceCollection Services { get; private set; }

        public FunctionsWorkerApplicationBuilder(IServiceCollection services)
        {
            Services = services;
            _pipelineBuilder = new DefaultInvocationPipelineBuilder<FunctionContext>();
            Services.AddSingleton<FunctionExecutionDelegate>(sp =>
            {
                return _pipelineBuilder.Build();
            });
        }

        public IFunctionsWorkerApplicationBuilder Use(Func<FunctionExecutionDelegate, FunctionExecutionDelegate> middleware)
        {
            _pipelineBuilder.Use(middleware);
            return this;
        }
    }
}
