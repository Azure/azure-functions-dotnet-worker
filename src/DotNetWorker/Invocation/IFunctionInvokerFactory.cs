using System.Reflection;

namespace Microsoft.Azure.Functions.DotNetWorker.Invocation
{
    internal interface IFunctionInvokerFactory
    {
        IFunctionInvoker Create(MethodInfo method);
    }
}
