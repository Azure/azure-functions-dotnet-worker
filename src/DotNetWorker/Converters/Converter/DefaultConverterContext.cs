using Microsoft.Azure.Functions.Worker.Definition;
using Microsoft.Azure.Functions.Worker.Pipeline;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class DefaultConverterContext : ConverterContext
    {
        public DefaultConverterContext(FunctionParameter parameter, object? source, FunctionExecutionContext context)
        {
            Parameter = parameter;
            Source = source;
            ExecutionContext = context;
        }

        public override object? Source { get; set; }

        public override FunctionParameter Parameter { get; set; }

        public override FunctionExecutionContext ExecutionContext { get; set; }
    }
}
