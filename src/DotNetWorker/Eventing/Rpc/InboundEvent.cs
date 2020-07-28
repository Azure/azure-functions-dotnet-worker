// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Grpc.Core;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    public class InboundEvent : RpcEvent
    {
        public IAsyncStreamReader<StreamingMessage> requestStream;

        public InboundEvent(string workerId, StreamingMessage message) : base(workerId, message, MessageOrigin.Worker)
        {
        }
    }
}
