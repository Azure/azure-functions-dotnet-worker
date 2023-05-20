using System.Threading.Channels;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace FunctionsNetHost.Grpc
{
    internal class MessageChannel
    {
        /// <summary>
        /// This channel holds messages meant to be send to the customer payload.
        /// </summary>
        private readonly Channel<StreamingMessage> _inboundChannel;

        private readonly Channel<StreamingMessage> _outChannel;


        private static readonly MessageChannel _instance = new();

        static MessageChannel()
        {
        }

        private MessageChannel()
        {
            var channelOptions = new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = false,
                AllowSynchronousContinuations = true
            };

            var channelOptions2 = new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = false,
                AllowSynchronousContinuations = true
            };

            _inboundChannel = Channel.CreateUnbounded<StreamingMessage>(channelOptions);
            _outChannel = Channel.CreateUnbounded<StreamingMessage>(channelOptions2);
        }

        public static MessageChannel Instance
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

        public Channel<StreamingMessage> OutboundChannel => _outChannel;


        /// <summary>
        /// Pushes a message to the inbound channel.
        /// </summary>
        public async Task SendInboundAsync(StreamingMessage inboundMessage)
        {
            await _inboundChannel.Writer.WriteAsync(inboundMessage);
        }

        public async Task SendOutboundAsync(StreamingMessage outboundMessage)
        {
            await _outChannel.Writer.WriteAsync(outboundMessage);
        }
    }
}
