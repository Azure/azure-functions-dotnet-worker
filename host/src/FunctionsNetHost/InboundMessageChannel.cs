using System.Threading.Channels;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace FunctionsNetHost
{
    internal class InboundMessageChannel
    {
        /// <summary>
        /// This channel holds messages meant to be send to the customer payload.
        /// </summary>
        private readonly Channel<StreamingMessage> _inboundChannel;

        private static readonly InboundMessageChannel _instance = new();

        static InboundMessageChannel()
        {
        }

        private InboundMessageChannel()
        {
            UnboundedChannelOptions channelOptions = new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = false,
                AllowSynchronousContinuations = true
            };

            _inboundChannel = Channel.CreateUnbounded<StreamingMessage>(channelOptions);
        }

        public static InboundMessageChannel Instance
        {
            get
            {
                return _instance;
            }
        }

        /// <summary>
        /// Messages which needs to go to customer payload gets pushed to this channel.
        /// </summary>
        public Channel<StreamingMessage> InboundChannel => _inboundChannel;

        /// <summary>
        /// Pushes a message to the inbound channel.
        /// </summary>
        public async Task SendAsync(StreamingMessage inboundMessage)
        {
            await _inboundChannel.Writer.WriteAsync(inboundMessage);
        }
    }
}
