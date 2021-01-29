namespace Microsoft.Azure.Functions.Worker.Context
{
    public interface IValueProvider
    {
        object? GetValue(string name);
    }
}
