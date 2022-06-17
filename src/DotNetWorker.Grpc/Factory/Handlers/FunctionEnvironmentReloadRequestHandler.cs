using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc.Factory.Contracts;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Grpc.Factory.Handlers;

internal class FunctionEnvironmentReloadRequestHandler : IGrpcWorkerMessageHandler
{
    public Task<StreamingMessage> HandleMessageAsync(StreamingMessage request)
    {
        var responseMessage = new StreamingMessage
        {
            RequestId = request.RequestId,
            FunctionEnvironmentReloadResponse = new FunctionEnvironmentReloadResponse
            {
                Result = new StatusResult
                {
                    Status = StatusResult.Types.Status.Success
                }
            }
        };

        return Task.FromResult(responseMessage);
    }
}
