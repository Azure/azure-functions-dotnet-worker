// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Channels;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace FunctionsNetHost.Grpc
{
    /// <summary>
    /// Bidirectional message channel meant to store inbound(to worker) and outbound(to host) messages.
    /// </summary>
    internal sealed class MessageChannel
    {
        private MessageChannel()
        {
            InboundChannel = Channel.CreateUnbounded<StreamingMessage>(CreateUnboundedChannelOptions());
            OutboundChannel = Channel.CreateUnbounded<StreamingMessage>(CreateUnboundedChannelOptions());
        }

        /// <summary>
        /// Gets the instances of the messaging channel.
        /// </summary>
        internal static MessageChannel Instance { get; } = new();

        /// <summary>
        /// Messages which needs to go to worker payload gets pushed to this channel.
        /// </summary>
        internal Channel<StreamingMessage> InboundChannel { get; }

        /// <summary>
        /// Messages which needs to go to host gets pushed to this channel.
        /// </summary>
        internal Channel<StreamingMessage> OutboundChannel { get; }

        /// <summary>
        /// Pushes a message to the inbound channel(to worker).
        /// </summary>
        internal async Task SendInboundAsync(StreamingMessage inboundMessage)
        {
            await InboundChannel.Writer.WriteAsync(inboundMessage);
        }

        /// <summary>
        /// Pushes a messages to the outbound channel(to host)
        /// </summary>
        internal async Task SendOutboundAsync(StreamingMessage outboundMessage)
        {
            await OutboundChannel.Writer.WriteAsync(outboundMessage);
        }
        
        private static UnboundedChannelOptions CreateUnboundedChannelOptions()
        {
            return new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = false,
                AllowSynchronousContinuations = true
            };
        }
    }
}
