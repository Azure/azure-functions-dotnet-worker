using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    public interface IFunctionBroker
    {
        void AddFunction(FunctionLoadRequest functionLoadRequest);
        Task<InvocationResponse> InvokeAsync(InvocationRequest invocationRequest);
    }
}
