using System;
using System.Threading.Channels;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    internal class FunctionsHostChannelManager
    {
        public FunctionsHostChannelManager(Channel<StreamingMessage> inputChannel, Channel<StreamingMessage> outputChannel)
        {
            InputChannel = inputChannel ?? throw new ArgumentNullException(nameof(inputChannel));
            OutputChannel = outputChannel ?? throw new ArgumentNullException(nameof(outputChannel));
        }

        public Channel<StreamingMessage> InputChannel { get; }

        public Channel<StreamingMessage> OutputChannel { get; }
    }
}
