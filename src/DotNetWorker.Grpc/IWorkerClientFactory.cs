// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Grpc
{
    internal interface IWorkerClientFactory
    {
        Task<IWorkerClient> StartClientAsync(IMessageProcessor messageProcessor, CancellationToken token);
    }
}