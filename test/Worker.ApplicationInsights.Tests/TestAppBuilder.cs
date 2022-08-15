using System;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights.Tests;

internal class TestAppBuilder : IFunctionsWorkerApplicationBuilder
{
    public IServiceCollection Services { get; } = new ServiceCollection();

    public IFunctionsWorkerApplicationBuilder Use(Func<FunctionExecutionDelegate, FunctionExecutionDelegate> middleware)
    {
        throw new NotImplementedException();
    }
}
