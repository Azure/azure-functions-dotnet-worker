﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Grpc
{
    internal interface IWorkerClientFactory
    {
        IWorkerClient CreateClient(IMessageProcessor messageProcessor);
    }
}
