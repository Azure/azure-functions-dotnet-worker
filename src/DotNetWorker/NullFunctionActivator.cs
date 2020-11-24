using System;

namespace Microsoft.Azure.Functions.Worker
{
    internal class NullFunctionActivator : IFunctionActivator
    {
        private NullFunctionActivator()
        {
        }

        public static NullFunctionActivator Instance { get; } = new NullFunctionActivator();

        public T CreateInstance<T>(IServiceProvider services)
        {
            return default(T);
        }
    }
}
