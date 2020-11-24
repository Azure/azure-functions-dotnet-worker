using Microsoft.Azure.Functions.Worker.Invocation;

namespace Microsoft.Azure.Functions.Worker
{
    public class FunctionDefinition
    {
        public FunctionMetadata? Metadata { get; set; }

        public IFunctionInvoker? Invoker { get; set; }
    }
}
