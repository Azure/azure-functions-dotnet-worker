using System.Threading.Tasks;
using Microsoft.Azure.Functions.DotNetWorker.Pipeline;

namespace Microsoft.Azure.Functions.DotNetWorker.Invocation
{
    internal interface IFunctionExecutor
    {
        Task ExecuteAsync(FunctionExecutionContext context);
    }
}
