using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    internal interface IHostRequestHandler
    {
        Task<WorkerInitResponse> InitializeWorkerAsync(WorkerInitRequest request);

        Task<FunctionLoadResponse> LoadFunctionAsync(FunctionLoadRequest request);

        Task<InvocationResponse> InvokeFunctionAsync(InvocationRequest request);

        Task<FunctionEnvironmentReloadResponse> ReloadEnvironmentAsync(FunctionEnvironmentReloadRequest request);
    }
}
