using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    internal interface IMethodInvoker<TInstance, TReturn>
    {
        // The cancellation token, if any, is provided along with the other arguments.
        Task<TReturn> InvokeAsync(TInstance instance, object[] arguments);
    }
}
