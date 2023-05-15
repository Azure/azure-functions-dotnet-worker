using System;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Extension methods for <see cref="IFunctionsWorkerApplicationBuilder"/>.
    /// </summary>
    public static class FunctionsWorkerApplicationBuilderExtensions
    {
        /// <summary>
        /// Gets the context for the <see cref="IFunctionsWorkerApplicationBuilder"/>.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static FunctionsWorkerApplicationBuilderContext GetContext(this IFunctionsWorkerApplicationBuilder builder)
        {
            if (builder is IFunctionsWorkerApplicationBuilderExt builderExt)
            {
                return builderExt.Context;
            }

            throw new InvalidOperationException($"Context is only available when calling {nameof(CoreWorkerHostBuilderExtensions.ConfigureFunctionsWorker)}.");
        }
    }
}
