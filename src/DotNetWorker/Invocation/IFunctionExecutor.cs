using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Pipeline;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    internal interface IFunctionExecutor
    {
        Task ExecuteAsync(FunctionExecutionContext context);
    }
}
