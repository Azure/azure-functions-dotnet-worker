using System;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    public class DefaultFunctionActivator : IFunctionActivator
    {
        public T CreateInstance<T>(IServiceProvider services)
        {
            return Activator.CreateInstance<T>();
        }
    }
}
