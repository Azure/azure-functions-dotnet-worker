using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.DotNetWorker.Invocation
{
    public interface IFunctionInvoker
    {
        object CreateInstance(IServiceProvider instanceServices);

        Task<object> InvokeAsync(object instance, object[] arguments);
    }
}
