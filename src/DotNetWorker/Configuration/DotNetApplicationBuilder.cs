using Microsoft.Azure.Functions.DotNetWorker.FunctionInvoker;
using Microsoft.Azure.Functions.DotNetWorker.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.Azure.Functions.DotNetWorker.Configuration
{
    class DotNetApplicationBuilder : IDotNetApplicationBuilder
    {
        private readonly IInvocationPipelineBuilder<FunctionExecutionContext> _pipelineBuilder;

        public IServiceCollection Services { get; private set; }

        public DotNetApplicationBuilder(IServiceCollection services)
        {
            Services = services;
            _pipelineBuilder = new DefaultInvocationPipelineBuilder<FunctionExecutionContext>();

            Services.AddSingleton<FunctionExecutionDelegate>(sp =>
            {
                _pipelineBuilder.Use(next => context =>
                {
                    IFunctionInvoker invoker = sp.GetService<IFunctionInvoker>();
                    return invoker.InvokeAsync(context);
                });

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
