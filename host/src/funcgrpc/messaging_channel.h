#pragma once
#include <boost/fiber/unbuffered_channel.hpp>
#include <future>
#include <iostream>

#include <grpc/byte_buffer.h>
#include <grpc/grpc.h>
#include <grpcpp/channel.h>
#include <grpcpp/client_context.h>
#include <grpcpp/create_channel.h>
#include <grpcpp/generic/generic_stub.h>

using grpc::ByteBuffer;

namespace funcgrpc
{
typedef boost::fibers::unbuffered_channel<ByteBuffer> channel_t;
typedef boost::fibers::channel_op_status channel_pop_status_t;

class MessageChannel
{
  private:
    MessageChannel() = default;

  public:
    static MessageChannel &GetInstance();

    /// <summary>
    /// Gets the outbound channel. Any messages which needs to go out (to the host)
    /// should be pushed to this channel.
    /// </summary>
    /// <returns></returns>
    channel_t &GetOutboundChannel();

    /// <summary>
    /// Gets the inbound channel.
    /// Call pop() on this channel to get the messages coming to the worker from host.
    /// Invocation request is an example message coming through this channel.
    /// Example use:
    /// StreamingMessage message;
    /// while (channel_pop_status_t::success == outboundChannel.pop(message)) {
    ///     // Do something with the message
    /// }
    /// </summary>
    /// <returns></returns>
    channel_t &GetInboundChannel();
};
} // namespace funcgrpc