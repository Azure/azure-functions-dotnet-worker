using System.Reflection;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    internal interface IMethodInvokerFactory
    {
        IMethodInvoker<TInstance, TReturn> Create<TInstance, TReturn>(MethodInfo method);
    }
}
