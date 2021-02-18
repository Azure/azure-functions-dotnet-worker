namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class FunctionContextConverter : IConverter
    {
        public bool TryConvert(ConverterContext context, out object? target)
        {
            target = null;

            // Special handling for the context.
            if (context.Parameter.Type == typeof(FunctionContext))
            {
                target = context.FunctionContext;
                return true;
            }

            return false;
        }
    }
}
