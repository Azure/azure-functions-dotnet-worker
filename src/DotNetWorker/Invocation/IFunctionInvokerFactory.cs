using System.Reflection;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    internal interface IFunctionInvokerFactory
    {
        IFunctionInvoker Create(MethodInfo method);
    }
}
