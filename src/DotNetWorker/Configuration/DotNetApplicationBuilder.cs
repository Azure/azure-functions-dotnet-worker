using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.DotNetWorker.Configuration
{
    class DotNetApplicationBuilder : IDotNetApplicationBuilder
    {
        public IServiceCollection Services { get; private set; }

        public DotNetApplicationBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }
}
