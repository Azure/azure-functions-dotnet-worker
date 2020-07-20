using System;
using Microsoft.Extensions.DependencyInjection;

namespace FunctionsDotNetWorker.Configuration
{
    public interface IDotNetApplicationBuilder
    {
        IServiceCollection Services { get; }
    }
}
