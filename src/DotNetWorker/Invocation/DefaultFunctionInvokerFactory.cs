using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.DotNetWorker.Invocation
{
    internal class DefaultFunctionInvokerFactory : IFunctionInvokerFactory
    {
        private readonly IMethodInvokerFactory _methodInvokerFactory;
        private readonly IFunctionActivator _functionActivator;
        private IFunctionActivator functionActivator;

        public DefaultFunctionInvokerFactory(IMethodInvokerFactory methodInvokerFactory, IFunctionActivator functionActivator)
        {
            _methodInvokerFactory = methodInvokerFactory ?? throw new ArgumentNullException(nameof(methodInvokerFactory));
            _functionActivator = functionActivator ?? throw new ArgumentNullException(nameof(functionActivator));
        }

        public IFunctionInvoker Create(MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            Type? reflectedType = method.ReflectedType;

            if (reflectedType == null)
            {
                throw new InvalidOperationException("No reflected type was found.");
            }

            MethodInfo genericMethodDefinition = typeof(DefaultFunctionInvokerFactory).GetMethod(nameof(DefaultFunctionInvokerFactory.CreateGeneric), BindingFlags.NonPublic | BindingFlags.Static)!;

            if (!TryGetReturnType(method, out Type? returnType))
            {
                returnType = typeof(object);
            }

            MethodInfo genericMethod = genericMethodDefinition.MakeGenericMethod(reflectedType, returnType!);


            IFunctionActivator activator = _functionActivator;
            if (method.IsStatic)
            {
                activator = NullFunctionActivator.Instance;
            }

            var lambda = (Func<MethodInfo, IMethodInvokerFactory, IFunctionActivator, IFunctionInvoker>)Delegate.CreateDelegate(typeof(Func<MethodInfo, IMethodInvokerFactory, IFunctionActivator, IFunctionInvoker>), genericMethod);
            return lambda.Invoke(method, _methodInvokerFactory, activator);
        }

        private static IFunctionInvoker CreateGeneric<TReflected, TReturnValue>(MethodInfo method, IMethodInvokerFactory methodInvokerFactory, IFunctionActivator functionActivator)
        {
            List<string?> parameterNames = method.GetParameters().Select(p => p.Name).ToList();

            IMethodInvoker<TReflected, TReturnValue> methodInvoker = methodInvokerFactory.Create<TReflected, TReturnValue>(method);

            return new DefaultFunctionInvoker<TReflected, TReturnValue>(methodInvoker, functionActivator);
        }

        private static bool TryGetReturnType(MethodInfo methodInfo, out Type? type)
        {
            Type returnType = methodInfo.ReturnType;
            if (returnType == typeof(void) || returnType == typeof(Task))
            {
                type = null;
            }
            else if (typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
            {
                type = returnType.GetGenericArguments()[0];
            }
            else
            {
                type = returnType;
            }

            return type != null;
        }
    }
}
