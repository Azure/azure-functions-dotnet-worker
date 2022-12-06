#include "byte_buffer_helper.h"
#include <memory>

// these bytebuffer helpers were copied from e2e tests of https://github.com/grpc/grpc

std::unique_ptr<ByteBuffer> funcgrpc::SerializeToByteBuffer(protobuf::Message *message)
{
    grpc::string buf;
    message->SerializePartialToString(&buf);
    Slice slice(buf);

    return std::make_unique<ByteBuffer>(&slice, 1);
}

bool funcgrpc::ParseFromByteBuffer(ByteBuffer *buffer, protobuf::Message *message)
{
    std::vector<Slice> slices;
    (void)buffer->Dump(&slices);
    grpc::string buf;
    buf.reserve(buffer->Length());
    for (auto s = slices.begin(); s != slices.end(); s++)
    {
        buf.append(reinterpret_cast<const char *>(s->begin()), s->size());
    }

    return message->ParseFromString(buf);
}

std::unique_ptr<ByteBuffer> funcgrpc::SerializeToByteBufferFromChar(char *managedMessage, int managedMessageSize)
{
    grpc::string buf(managedMessage, managedMessageSize);
    Slice slice(buf);

    return std::make_unique<ByteBuffer>(&slice, 1);
}

string funcgrpc::ParseFromByteBufferToString(ByteBuffer *buffer)
{
    std::vector<Slice> slices;
    (void)buffer->Dump(&slices);
    grpc::string buf;
    buf.reserve(buffer->Length());
    for (auto s = slices.begin(); s != slices.end(); s++)
    {
        buf.append(reinterpret_cast<const char *>(s->begin()), s->size());
    }

    return buf;
}
