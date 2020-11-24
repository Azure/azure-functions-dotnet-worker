using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.Azure.Functions.Worker.Configuration
{
    public interface IFunctionsWorkerApplicationBuilder
    {
        IServiceCollection Services { get; }

        IFunctionsWorkerApplicationBuilder Use(Func<FunctionExecutionDelegate, FunctionExecutionDelegate> middleware);
    }
}
