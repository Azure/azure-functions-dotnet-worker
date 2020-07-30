using System;
using System.Threading.Channels;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.DotNetWorker
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
