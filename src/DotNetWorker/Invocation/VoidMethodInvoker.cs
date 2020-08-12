using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.DotNetWorker.Invocation
{
    internal class VoidMethodInvoker<TReflected, TReturnValue> : IMethodInvoker<TReflected, TReturnValue>
    {
        private readonly Action<TReflected, object[]> _lambda;

        public VoidMethodInvoker(Action<TReflected, object[]> lambda)
        {
            _lambda = lambda;
        }

        public Task<TReturnValue> InvokeAsync(TReflected instance, object[] arguments)
        {
            _lambda.Invoke(instance, arguments);
            return Task.FromResult(default(TReturnValue));
        }
    }
}
