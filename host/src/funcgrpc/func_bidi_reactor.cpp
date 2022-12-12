// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "func_bidi_reactor.h"
#include "byte_buffer_helper.h"
#include "func_log.h"
#include "handlers/funcgrpc_native_handler.h"
#include "messaging_channel.h"

funcgrpc::FunctionBidiReactor::FunctionBidiReactor(GrpcWorkerStartupOptions *pOptions,
                                                   NativeHostApplication *pApplication)
{
    pOptions_ = pOptions;
    pApplication_ = pApplication;

    std::string endpoint = pOptions->host + ":" + std::to_string(pOptions->port);
    grpc::ChannelArguments channelArgs;
    channelArgs.SetInt(GRPC_ARG_MAX_SEND_MESSAGE_LENGTH, pOptions->grpcMaxMessageLength);
    channelArgs.SetInt(GRPC_ARG_MAX_RECEIVE_MESSAGE_LENGTH, pOptions->grpcMaxMessageLength);
    auto channel = grpc::CreateCustomChannel(endpoint, grpc::InsecureChannelCredentials(), channelArgs);

    auto generic_stub_ = make_unique<grpc::GenericStub>(channel);
    const char *suffix_for_stats = nullptr;
    grpc::StubOptions options(suffix_for_stats);
    generic_stub_->PrepareBidiStreamingCall(&client_context_, "/AzureFunctionsRpcMessages.FunctionRpc/EventStream",
                                            options, this);
    handler_ = std::unique_ptr<MessageHandler>(new NativeHostMessageHandler(pApplication));

    sendStartStream();
    StartRead(&read_);
    StartCall();

    auto outboundWriterTask = std::async(std::launch::async, [this]() { startOutboundWriter(); });
    auto inboundMsgHandlingTask = std::async(std::launch::async, [this]() { handleInboundMessagesForApplication(); });
}

void funcgrpc::FunctionBidiReactor::OnWriteDone(bool ok)
{
    FUNC_LOG_TRACE("OnWriteDone. ok:{}", ok);

    {
        bool expect = true;
        if (!write_inprogress_.compare_exchange_strong(expect, false, std::memory_order_relaxed))
        {
            FUNC_LOG_WARN("Illegal write_inprogress_ state");
        }
    }

    fireWrite();
}

void funcgrpc::FunctionBidiReactor::OnReadDone(bool ok)
{
    if (!ok)
    {
        FUNC_LOG_WARN("Failed to read response.");
        return;
    }

    grpc::ByteBuffer outboundMessage(read_);

    auto handleMsgTask = std::async(std::launch::async, [this, &outboundMessage]() {
        auto outboundStreamingMsg = StreamingMessage();
        handler_->HandleMessage(&outboundMessage);
    });

    fireRead();
}

void funcgrpc::FunctionBidiReactor::OnDone(const grpc::Status &status)
{
    if (status.ok())
    {
        FUNC_LOG_DEBUG("Bi-directional stream ended. status.code={}, status.message={}", status.error_code(),
                       status.error_message());
    }

    std::unique_lock<std::mutex> l(mu_);
    status_ = status;
    done_ = true;
    cv_.notify_one();
}

Status funcgrpc::FunctionBidiReactor::Await()
{
    std::unique_lock<std::mutex> l(mu_);
    cv_.wait(l, [this] { return done_; });
    return std::move(status_);
}

/// <summary>
/// Manages outbound(to host) write operations.
/// Listening to the outbound channel and when a new message arrives,
/// we will push that entry to the write buffer.
/// </summary>

void funcgrpc::FunctionBidiReactor::startOutboundWriter()
{
    FUNC_LOG_DEBUG("startOutboundWriter started");
    auto &outboundChannel = funcgrpc::MessageChannel::GetInstance().GetOutboundChannel();

    grpc::ByteBuffer messagetoSend;
    while (channel_pop_status_t::success == outboundChannel.pop(messagetoSend))
    {
        FUNC_LOG_DEBUG("Popped new message received in outbound channel");
        writeToOutboundBuffer(messagetoSend);
    }
    FUNC_LOG_WARN("exiting startOutboundWriter.");
}

/// <summary>
/// Sends the startStream message to host.
/// This will initiate the GRPC communication with host.
/// </summary>
void funcgrpc::FunctionBidiReactor::sendStartStream()
{
    StreamingMessage startStream;
    startStream.mutable_start_stream()->set_worker_id(pOptions_->workerId);
    FUNC_LOG_INFO("Sending StartStream message.");

    auto bbUniqPtr = funcgrpc::SerializeToByteBuffer(&startStream);
    auto bbPtr = bbUniqPtr.get();
    writeToOutboundBuffer(*bbPtr);
}

/// <summary>
/// Pushes an outgoing message(to host) to the buffer.
/// </summary>
void funcgrpc::FunctionBidiReactor::writeToOutboundBuffer(const grpc::ByteBuffer &outgoingMessage)
{
    {
        absl::MutexLock lk(&writes_mtx_);
        writes_.push_back(outgoingMessage);
        FUNC_LOG_TRACE("Pushed entry to writes_ buffer");
    }
    fireWrite();
}

/// <summary>
/// Pull the message from buffer and writes to GRPC outbound stream.
/// </summary>
void funcgrpc::FunctionBidiReactor::fireWrite()
{
    {
        absl::MutexLock lk(&writes_mtx_);
        if (writes_.empty())
        {
            return;
        }

        bool expect = false;
        if (write_inprogress_.compare_exchange_strong(expect, true, std::memory_order_relaxed))
        {
            write_ = *writes_.begin();
            writes_.erase(writes_.begin());
        }
        else
        {
            FUNC_LOG_DEBUG("Another write operation is in progress.");
            return;
        }
    }

    StartWrite(&write_);
}

void funcgrpc::FunctionBidiReactor::fireRead()
{
    FUNC_LOG_TRACE("fireRead called");
    StartRead(&read_);
}

/// <summary>
/// Handles messages meant for the application/dotnet worker.
/// Listening to the inbound channel and when a new message arrives,
/// we will send that to the application_.
///
/// TO DO: I think we can move this to the funcgrpc_native_handler.cpp.
///
/// </summary>
void funcgrpc::FunctionBidiReactor::handleInboundMessagesForApplication()
{
    FUNC_LOG_DEBUG("handleInboundMessagesForApplication started");

    auto &inboundChannel = funcgrpc::MessageChannel::GetInstance().GetInboundChannel();
    grpc::ByteBuffer ibByteBuffer;

    while (channel_pop_status_t::success == inboundChannel.pop(ibByteBuffer))
    {
        FUNC_LOG_DEBUG("Popped new message received in inbound channel");

        auto size = ibByteBuffer.Length();
        std::string t = funcgrpc::ParseFromByteBufferToString(&ibByteBuffer);
        auto charArr = t.c_str();
        auto *unsignedCharArr = (unsigned char *)charArr;

        pApplication_->HandleIncomingMessage(unsignedCharArr, size);
    }

    FUNC_LOG_WARN("exiting handleInboundMessagesForApplication");
}