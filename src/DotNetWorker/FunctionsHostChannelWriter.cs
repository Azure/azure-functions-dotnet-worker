using System;
using System.Threading.Channels;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    /// <summary>
    /// Used for writing gRpc messages to the Functions Host.
    /// </summary>
    internal class FunctionsHostChannelWriter
    {
        private readonly FunctionsHostChannelManager _channelManager;

        public FunctionsHostChannelWriter(FunctionsHostChannelManager channelManager)
        {
            _channelManager = channelManager ?? throw new ArgumentNullException(nameof(channelManager));
        }

        public ChannelWriter<StreamingMessage> Writer => _channelManager.OutputChannel.Writer;
    }
}
