using System.Threading.Tasks;
using Microsoft.Azure.Functions.DotNetWorker.FunctionInvoker;

namespace Microsoft.Azure.Functions.DotNetWorker.Pipeline
{
    internal class FunctionExecutionMiddleware
    {
        IFunctionInvoker _functionInvoker;

        public FunctionExecutionMiddleware(IFunctionInvoker functionInvoker)
        {
            _functionInvoker = functionInvoker;
        }

        public Task Invoke(FunctionExecutionContext context)
        {
            return _functionInvoker.InvokeAsync(context);
        }
    }
}
