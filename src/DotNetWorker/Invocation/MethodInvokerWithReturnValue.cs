using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.DotNetWorker.Invocation
{
    internal class MethodInvokerWithReturnValue<TReflected, TReturnValue> : IMethodInvoker<TReflected, TReturnValue>
    {
        private readonly Func<TReflected, object[], TReturnValue> _lambda;

        public MethodInvokerWithReturnValue(Func<TReflected, object[], TReturnValue> lambda)
        {
            _lambda = lambda;
        }

        public Task<TReturnValue> InvokeAsync(TReflected instance, object[] arguments)
        {
            TReturnValue result = _lambda.Invoke(instance, arguments);
            return Task.FromResult(result);
        }
    }
}
