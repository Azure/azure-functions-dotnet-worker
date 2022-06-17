using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc.Factory.Contracts;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Grpc.Factory.Handlers;
internal class WorkerInitRequestHandler : IGrpcWorkerMessageHandler
{
    public Task<StreamingMessage> HandleMessageAsync(StreamingMessage request)
    {
        var responseMessage = new StreamingMessage
        {
            RequestId = request.RequestId
        };

        var response = new WorkerInitResponse
        {
            Result = new StatusResult { Status = StatusResult.Types.Status.Success },
            WorkerVersion = WorkerInformation.Instance.WorkerVersion
        };

        response.Capabilities.Add("RpcHttpBodyOnly", bool.TrueString);
        response.Capabilities.Add("RawHttpBodyBytes", bool.TrueString);
        response.Capabilities.Add("RpcHttpTriggerMetadataRemoved", bool.TrueString);
        response.Capabilities.Add("UseNullableValueDictionaryForHttp", bool.TrueString);
        response.Capabilities.Add("TypedDataCollection", bool.TrueString);
        response.Capabilities.Add("WorkerStatus", bool.TrueString);
        response.Capabilities.Add("HandlesWorkerTerminateMessage", bool.TrueString);

        responseMessage.WorkerInitResponse = response;
        return Task.FromResult(responseMessage);
    }
}
