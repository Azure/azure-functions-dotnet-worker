namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class ExactMatchConverter : IConverter
    {
        public bool TryConvert(ConverterContext context, out object? target)
        {
            if (context.Source.GetType() == context.Parameter.Type)
            {
                target = context.Source;
                return true;
            }

            target = default;
            return false;
        }
    }
}
