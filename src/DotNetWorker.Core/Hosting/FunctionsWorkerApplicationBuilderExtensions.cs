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
        /// <param name="builder">The <see cref="IFunctionsWorkerApplicationBuilder"/>.</param>
        /// <returns>The <see cref="FunctionsWorkerApplicationBuilderContext"/> associated with the <see cref="IFunctionsWorkerApplicationBuilder"/>.</returns>
        public static FunctionsWorkerApplicationBuilderContext GetContext(this IFunctionsWorkerApplicationBuilder builder)
        {
            return (builder as IFunctionsWorkerApplicationBuilderContextProvider)?.Context ??
                new FunctionsWorkerApplicationBuilderContext();
        }
    }
}
