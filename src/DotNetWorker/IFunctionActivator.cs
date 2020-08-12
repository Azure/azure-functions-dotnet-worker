using System;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    public interface IFunctionActivator
    {
        T CreateInstance<T>(IServiceProvider services);
    }
}
