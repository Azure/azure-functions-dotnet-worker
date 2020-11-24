using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    internal class VoidTaskMethodInvoker<TReflected, TReturnType> : IMethodInvoker<TReflected, TReturnType>
    {
        private readonly Func<TReflected, object[], Task> _lambda;

        public VoidTaskMethodInvoker(Func<TReflected, object[], Task> lambda)
        {
            _lambda = lambda;
        }

        public async Task<TReturnType> InvokeAsync(TReflected instance, object[] arguments)
        {
            await _lambda.Invoke(instance, arguments);
            return default;
        }
    }
}
