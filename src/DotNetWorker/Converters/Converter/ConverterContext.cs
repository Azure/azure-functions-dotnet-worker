using Microsoft.Azure.Functions.Worker.Definition;
using Microsoft.Azure.Functions.Worker.Pipeline;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal abstract class ConverterContext
    {
        public abstract FunctionParameter Parameter { get; set; }

        public abstract object Source { get; set; }

        public abstract FunctionExecutionContext ExecutionContext { get; set; }
    }
}