#ifndef FUNC_NATIVEHANDLER
#define FUNC_NATIVEHANDLER

#include "../funcgrpc.h"
#include "../funcgrpc_handlers.h"
#include "../nativehostapplication.h"
#include "grpcpp/support/byte_buffer.h"
#include <grpc/byte_buffer.h>
#include <grpc/grpc.h>
#include <grpcpp/channel.h>
#include <grpcpp/client_context.h>
#include <grpcpp/create_channel.h>
#include <grpcpp/generic/generic_stub.h>

using namespace AzureFunctionsRpc;

using grpc::ByteBuffer;
namespace AzureFunctionsRpc
{

class NativeHostMessageHandler : public MessageHandler
{

  public:
    explicit NativeHostMessageHandler(NativeHostApplication *application);

    void HandleMessage(ByteBuffer *receivedMessage) override;

  private:
    NativeHostApplication *application_;
    bool specializationRequestReceived = false;
};
} // namespace AzureFunctionsRpc

#endif