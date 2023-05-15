using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker
{
    internal class DefaultFunctionsWorkerApplicationBuilderContext : FunctionsWorkerApplicationBuilderContext
    {
        public DefaultFunctionsWorkerApplicationBuilderContext(IHostBuilder hostBuilder, HostBuilderContext context)
            : base(hostBuilder, context)
        {
        }
    }
}
