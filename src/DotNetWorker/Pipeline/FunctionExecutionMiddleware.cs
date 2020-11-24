using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Invocation;

namespace Microsoft.Azure.Functions.Worker.Pipeline
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
