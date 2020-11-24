using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    internal class DefaultFunctionInvoker<TInstance, TReturn> : IFunctionInvoker
    {
        private readonly IMethodInvoker<TInstance, TReturn> _methodInvoker;

        public DefaultFunctionInvoker(IMethodInvoker<TInstance, TReturn> methodInvoker, IFunctionActivator functionActivator)
        {
            _methodInvoker = methodInvoker ?? throw new ArgumentNullException(nameof(methodInvoker));
            FunctionActivator = functionActivator ?? throw new ArgumentNullException(nameof(functionActivator));
        }

        // For testing
        internal IFunctionActivator FunctionActivator { get; }

        public object CreateInstance(IServiceProvider instanceServices)
        {
            return FunctionActivator.CreateInstance<TInstance>(instanceServices);
        }

        public Task<object> InvokeAsync(object instance, object[] arguments)
        {
            return _methodInvoker.InvokeAsync((TInstance)instance, arguments)
                .ContinueWith(t => (object)t.Result);
        }
    }
}
