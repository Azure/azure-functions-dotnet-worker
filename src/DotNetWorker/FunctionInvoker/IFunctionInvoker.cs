using Microsoft.Azure.Functions.DotNetWorker.Pipeline;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.DotNetWorker.FunctionInvoker
{
    interface IFunctionInvoker
    {
        Task InvokeAsync(FunctionExecutionContext context);
    }
}
