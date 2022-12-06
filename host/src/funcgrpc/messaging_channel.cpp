#include "messaging_channel.h"
#include <iostream>

namespace funcgrpc
{
std::unique_ptr<channel_t> outboundChannelPtr = std::make_unique<channel_t>();
std::unique_ptr<channel_t> inboundChannelPtr = std::make_unique<channel_t>();

MessageChannel &MessageChannel::GetInstance()
{
    static MessageChannel single;
    return single;
}

channel_t &MessageChannel::GetOutboundChannel()
{
    return *outboundChannelPtr;
}

channel_t &MessageChannel::GetInboundChannel()
{
    return *inboundChannelPtr;
}
} // namespace funcgrpc