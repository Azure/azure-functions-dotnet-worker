using Microsoft.Azure.Functions.DotNetWorker.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.Azure.Functions.DotNetWorker.Configuration
{
    public interface IDotNetApplicationBuilder
    {
        IServiceCollection Services { get; }

        IDotNetApplicationBuilder Use(Func<FunctionExecutionDelegate, FunctionExecutionDelegate> middleware);
    }
}
