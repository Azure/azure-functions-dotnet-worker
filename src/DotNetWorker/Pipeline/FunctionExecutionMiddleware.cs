using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.DotNetWorker.Pipeline
{
    internal class FunctionExecutionMiddleware
    {
        public Task Invoke(FunctionExecutionContext context, FunctionExecutionDelegate functionExecutionDelegate)
        {
            return functionExecutionDelegate(context);
        }
    }
}
