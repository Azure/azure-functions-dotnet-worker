using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Context for the <see cref="IFunctionsWorkerApplicationBuilder"/>
    /// </summary>
    public class FunctionsWorkerApplicationBuilderContext
    {
        /// <summary>
        /// Gets or sets the <see cref="IHostBuilder"/> used to create the <see cref="IFunctionsWorkerApplicationBuilder"/>.
        /// This property will be null if there is no <see cref="IHostBuilder"/> available.
        /// </summary>
        public IHostBuilder? HostBuilder { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="HostBuilderContext"/> used to create the <see cref="IFunctionsWorkerApplicationBuilder"/>.
        /// This property will be null if there is no <see cref="HostBuilderContext"/> available.
        /// </summary>
        public HostBuilderContext? HostBuilderContext { get; set; }
    }
}
