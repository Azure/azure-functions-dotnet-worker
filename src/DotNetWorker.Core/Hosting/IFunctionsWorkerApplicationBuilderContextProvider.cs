namespace Microsoft.Azure.Functions.Worker
{
    internal interface IFunctionsWorkerApplicationBuilderContextProvider
    {
        FunctionsWorkerApplicationBuilderContext Context { get; }
    }
}
