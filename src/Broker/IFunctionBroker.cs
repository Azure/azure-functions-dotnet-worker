using System.Collections.Generic;
using FunctionsDotNetWorker.Logging;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace FunctionsDotNetWorker
{
    public interface IFunctionBroker
    {
        void AddFunction(FunctionLoadRequest functionLoadRequest);
        object Invoke(InvocationRequest invocationRequest, out List<ParameterBinding> parameterBindings, WorkerLogManager workerLogManager);
    }
}
