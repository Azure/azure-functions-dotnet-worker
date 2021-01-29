namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal interface IConverter
    {
        bool TryConvert(ConverterContext context, out object? target);
    }
}