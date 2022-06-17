using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Grpc.Factory.Contracts;

/// <summary>
/// IGrpcWorkerMessageHandler is the main interface for GRPC worker message handlers
/// </summary>
internal interface IGrpcWorkerMessageHandler
{
    /// <summary>
    /// HandleMessageAsync handles a <see cref="StreamingMessage"/> request and returns a new instance of <see cref="StreamingMessage"/>
    /// </summary>
    /// <param name="request"><see cref="StreamingMessage"/></param>
    /// <returns><see cref="StreamingMessage"/>Streaming message response</returns>
    Task<StreamingMessage> HandleMessageAsync(StreamingMessage request);
}
