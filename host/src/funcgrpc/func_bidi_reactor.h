#pragma once

#include <future>
#include <grpc/grpc.h>
#include <grpcpp/channel.h>
#include <grpcpp/client_context.h>
#include <grpcpp/create_channel.h>
#include <grpcpp/generic/generic_stub.h>
#include <grpc/byte_buffer.h>
#include <iostream>
#include <memory>
#include <mutex>
#include <string>
#include <thread>

#include "funcgrpc.h"
#include "funcgrpc_handlers.h"
#include "nativehostapplication.h"
#include <FunctionRpc.grpc.pb.h>
#include <FunctionRpc.pb.h>

using AzureFunctionsRpcMessages::FunctionLoadResponse;
using AzureFunctionsRpcMessages::FunctionRpc;
using AzureFunctionsRpcMessages::StartStream;
using AzureFunctionsRpcMessages::StatusResult;
using AzureFunctionsRpcMessages::StreamingMessage;
using AzureFunctionsRpcMessages::WorkerInitResponse;
using grpc::Channel;
using grpc::ClientContext;
using grpc::Status;
using grpc::ByteBuffer;

using namespace AzureFunctionsRpc;
using namespace grpc;
namespace funcgrpc
{

/// <summary>
/// BidiReactor implementation which reads and writes messages from the GRPC stream asynchronously.
/// See https://github.com/grpc/proposal/blob/master/L67-cpp-callback-api.md for details.
/// </summary>
class FunctionBidiReactor : public grpc::ClientBidiReactor<ByteBuffer, ByteBuffer>
{
  public:
    FunctionBidiReactor(GrpcWorkerStartupOptions *options, NativeHostApplication *application);

    void OnWriteDone(bool ok) override;

    void OnReadDone(bool ok) override;

    void OnDone(const Status &status) override;

    Status Await();

    void startOutboundWriter();

    void sendStartStream();

    void handleInboundMessagesForApplication();

    /// <summary>
    /// Push a new message to the buffer.
    /// </summary>
    /// <param name="outgoingMessage"></param>
    void writeToOutboundBuffer(const ByteBuffer &outgoingMessage);

    /// <summary>
    /// Writes the next message in buffer to the outbound GRPC stream.
    /// </summary>
    void fireWrite();

    /// <summary>
    /// Reads from GRPC stream.
    /// </summary>
    void fireRead();

  private:
    GrpcWorkerStartupOptions *pOptions_;
    std::unique_ptr<MessageHandler> handler_;
    NativeHostApplication *pApplication_;

    std::mutex mu_;
    std::condition_variable cv_;
    Status status_;
    bool done_ = false;

    grpc::ClientContext client_context_;

    /// <summary>
    /// Message to read from server.
    /// This should not be modified while a read operation( StartRead(&read_) ) is in progress.
    /// </summary>
	ByteBuffer read_;

    /// <summary>
    /// Message to write to server.
    /// This should not be modified while a write operation( StartWrite(&write_) ) is in progress.
    /// </summary>
	ByteBuffer write_;

    std::atomic_bool write_inprogress_{false};

    // Buffer for writing operations.
    std::vector<ByteBuffer> writes_ GUARDED_BY(writes_mtx_);
    absl::Mutex writes_mtx_;
};
} // namespace funcgrpc