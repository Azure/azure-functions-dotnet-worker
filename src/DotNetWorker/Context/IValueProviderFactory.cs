namespace Microsoft.Azure.Functions.Worker.Context
{
    internal interface IValueProviderFactory
    {
        IValueProvider Create();
    }
}
