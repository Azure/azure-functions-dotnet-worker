// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Grpc;

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
