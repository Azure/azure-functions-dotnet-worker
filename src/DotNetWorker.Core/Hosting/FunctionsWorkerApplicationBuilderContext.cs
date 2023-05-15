using System;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Context for the <see cref="IFunctionsWorkerApplicationBuilder"/>
    /// </summary>
    public abstract class FunctionsWorkerApplicationBuilderContext
    {
        private readonly IHostBuilder _hostBuilder;
        private readonly HostBuilderContext _context;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="hostBuilder">The host builder.</param>
        public FunctionsWorkerApplicationBuilderContext(IHostBuilder hostBuilder, HostBuilderContext context)
        {
            _hostBuilder = hostBuilder ?? throw new ArgumentNullException(nameof(hostBuilder));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// The <see cref="IHostBuilder"/> used to create the <see cref="IFunctionsWorkerApplicationBuilder"/>.
        /// </summary>
        public IHostBuilder HostBuilder => _hostBuilder;

        /// <summary>
        /// The <see cref="HostBuilderContext"/> used to create the <see cref="IFunctionsWorkerApplicationBuilder"/>.
        /// </summary>
        public HostBuilderContext HostBuilderContext => _context;
    }
}
