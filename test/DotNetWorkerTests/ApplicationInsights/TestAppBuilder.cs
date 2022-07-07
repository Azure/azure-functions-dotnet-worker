using System;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Tests.ApplicationInsights;

internal class TestAppBuilder : IFunctionsWorkerApplicationBuilder
{
    public int MiddlewareCount { get; private set; }

    public IServiceCollection Services { get; } = new ServiceCollection();

    public IFunctionsWorkerApplicationBuilder Use(Func<FunctionExecutionDelegate, FunctionExecutionDelegate> middleware)
    {
        MiddlewareCount++;
        return this;
    }
}
