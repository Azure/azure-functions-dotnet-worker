using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc.Factory.Contracts;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker.Grpc.Factory.Handlers;

internal class WorkerTerminateHandler : IGrpcWorkerMessageHandler
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public WorkerTerminateHandler(IHostApplicationLifetime hostApplicationLifetime)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    public Task<StreamingMessage> HandleMessageAsync(StreamingMessage request)
    {
        _hostApplicationLifetime.StopApplication();

        return Task.FromResult(new StreamingMessage
        {
            RequestId = request.RequestId,
        });
    }
}
