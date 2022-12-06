
#ifndef FUNCTIONSNETHOST_BYTE_BUFFER_HELPER_H
#define FUNCTIONSNETHOST_BYTE_BUFFER_HELPER_H

#include "grpcpp/impl/codegen/config_protobuf.h"
#include "grpcpp/support/byte_buffer.h"
#include "iostream"
#include <grpc/byte_buffer.h>
#include <grpc/grpc.h>

using namespace std;
using namespace grpc;
namespace funcgrpc
{
std::unique_ptr<grpc::ByteBuffer> SerializeToByteBuffer(grpc::protobuf::Message *message);

bool ParseFromByteBuffer(grpc::ByteBuffer *buffer, grpc::protobuf::Message *message);

string ParseFromByteBufferToString(ByteBuffer *buffer);

std::unique_ptr<ByteBuffer> SerializeToByteBufferFromChar(char *managedMessage, int managedMessageSize);
}
#endif
