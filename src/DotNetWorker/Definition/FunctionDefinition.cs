using Microsoft.Azure.Functions.DotNetWorker.Invocation;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    public class FunctionDefinition
    {
        public FunctionMetadata? Metadata { get; set; }

        public IFunctionInvoker? Invoker { get; set; }
    }
}
