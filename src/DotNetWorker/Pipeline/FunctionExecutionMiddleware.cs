using System.Threading.Tasks;
using Microsoft.Azure.Functions.DotNetWorker.Invocation;

namespace Microsoft.Azure.Functions.DotNetWorker.Pipeline
{
    internal class FunctionExecutionMiddleware
    {
        IFunctionExecutor _functionExecutor;

        public FunctionExecutionMiddleware(IFunctionExecutor functionExecutor)
        {
            _functionExecutor = functionExecutor;
        }

        public Task Invoke(FunctionExecutionContext context)
        {
            return _functionExecutor.ExecuteAsync(context);
        }
    }
}
