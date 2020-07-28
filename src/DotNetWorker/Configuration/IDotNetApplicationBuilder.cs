using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.DotNetWorker.Configuration
{
    public interface IDotNetApplicationBuilder
    {
        IServiceCollection Services { get; }
    }
}
