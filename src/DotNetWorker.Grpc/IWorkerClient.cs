﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Grpc
{
    internal interface IWorkerClient
    {
        Task StartAsync(CancellationToken cancellationToken);

        ValueTask SendMessageAsync(StreamingMessage message);
    }
}
