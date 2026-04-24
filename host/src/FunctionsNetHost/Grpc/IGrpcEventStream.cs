// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Grpc.Core;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace FunctionsNetHost.Grpc
{
    internal interface IGrpcEventStream : IDisposable
    {
        IAsyncStreamReader<StreamingMessage> ResponseStream { get; }

        IClientStreamWriter<StreamingMessage> RequestStream { get; }
    }
}
