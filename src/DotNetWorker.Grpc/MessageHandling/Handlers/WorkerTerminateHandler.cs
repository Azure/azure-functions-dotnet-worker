// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker.Grpc;

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
