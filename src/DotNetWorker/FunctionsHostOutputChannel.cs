// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Channels;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker
{
    internal class FunctionsHostOutputChannel
    {
        public FunctionsHostOutputChannel(Channel<StreamingMessage> channel)
        {
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        public Channel<StreamingMessage> Channel { get; }
    }
}
