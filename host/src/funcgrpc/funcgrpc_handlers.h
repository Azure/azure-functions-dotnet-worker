#ifndef FUNC_HANDLERS
#define FUNC_HANDLERS

#include <FunctionRpc.pb.h>
#include <grpc/grpc.h>
#include <grpcpp/channel.h>
#include <grpcpp/client_context.h>
#include <grpcpp/create_channel.h>
#include <grpcpp/generic/generic_stub.h>
#include <grpc/byte_buffer.h>

using AzureFunctionsRpcMessages::StreamingMessage;
using grpc::ByteBuffer;
namespace AzureFunctionsRpc
{

class MessageHandler
{
  public:
    virtual void HandleMessage(ByteBuffer *receivedMessage){};
};

} // namespace AzureFunctionsRpc

#endif