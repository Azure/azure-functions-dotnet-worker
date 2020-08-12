using System;

namespace Microsoft.Azure.Functions.DotNetWorker
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
