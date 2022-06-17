using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc.Factory.Contracts;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Grpc.Factory.Handlers;

internal class WorkerStatusResponseHandler : IGrpcWorkerMessageHandler
{
    public Task<StreamingMessage> HandleMessageAsync(StreamingMessage request)
    {
        var responseMessage = new StreamingMessage
        {
            RequestId = request.RequestId,
            WorkerStatusResponse = new WorkerStatusResponse()
        };

        return Task.FromResult(responseMessage);
    }
}
