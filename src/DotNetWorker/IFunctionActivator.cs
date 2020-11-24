using System;

namespace Microsoft.Azure.Functions.Worker
{
    public interface IFunctionActivator
    {
        T CreateInstance<T>(IServiceProvider services);
    }
}
