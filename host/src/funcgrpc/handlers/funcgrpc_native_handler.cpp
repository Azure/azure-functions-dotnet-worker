#include "funcgrpc_native_handler.h"
#include "../byte_buffer_helper.h"
#include "../func_log.h"
#include "../func_perf_marker.h"
#include "../funcgrpc_worker_config_handle.h"

using namespace AzureFunctionsRpc;
using AzureFunctionsRpcMessages::FunctionEnvironmentReloadResponse;
using AzureFunctionsRpcMessages::FunctionLoadResponse;
using AzureFunctionsRpcMessages::FunctionMetadataResponse;
using AzureFunctionsRpcMessages::StartStream;
using AzureFunctionsRpcMessages::StatusResult;
using AzureFunctionsRpcMessages::WorkerInitResponse;

using namespace std;

AzureFunctionsRpc::NativeHostMessageHandler::NativeHostMessageHandler(NativeHostApplication *application)
    : MessageHandler()
{
    application_ = application;
}

void AzureFunctionsRpc::NativeHostMessageHandler::HandleMessage(ByteBuffer *receivedMessageBb)
{
    if (specializationRequestReceived)
    {
        // Once we received specialization request & returned a response for that,
        // We do not need to deserialize the byte buffer version of message.
        FUNC_LOG_DEBUG("New message received in handler. Pushing to InboundChannel.");

        // Forward to inbound channel(managed code wrapper is listening to that channel).
        funcgrpc::MessageChannel::GetInstance().GetInboundChannel().push(*receivedMessageBb);
    }
    else
    {
        // We will deserialize the bytebuffer version as we need some property values.
        StreamingMessage receivedMessage;
        funcgrpc::ParseFromByteBuffer(receivedMessageBb, &receivedMessage);
        StreamingMessage::ContentCase contentCase = receivedMessage.content_case();
        FUNC_LOG_DEBUG("New message received. contentCase: {}", contentCase);

        if (contentCase == StreamingMessage::ContentCase::kWorkerInitRequest)
        {
            StreamingMessage streamingMsg;
            streamingMsg.mutable_worker_init_response()->mutable_result()->set_status(
                AzureFunctionsRpcMessages::StatusResult::Success);
            streamingMsg.mutable_worker_init_response()->set_worker_version("1.0.0.2");
            auto uPtrBb = funcgrpc::SerializeToByteBuffer(&streamingMsg);
            auto byteBuffer = uPtrBb.get();

            FUNC_LOG_DEBUG("Pushing response to OutboundChannel.contentCase: {}", streamingMsg.content_case());
            funcgrpc::MessageChannel::GetInstance().GetOutboundChannel().push(*byteBuffer);
        }
        else if (contentCase == StreamingMessage::ContentCase::kFunctionsMetadataRequest)
        {
            StreamingMessage streamingMsg;
            streamingMsg.mutable_function_metadata_response()->mutable_result()->set_status(
                AzureFunctionsRpcMessages::StatusResult::Success);
            streamingMsg.mutable_function_metadata_response()->set_use_default_metadata_indexing(true);
            auto uPtrBb = funcgrpc::SerializeToByteBuffer(&streamingMsg);
            auto byteBuffer = uPtrBb.get();

            FUNC_LOG_DEBUG("Pushing response to outbound channel.contentCase: {}", streamingMsg.content_case());
            funcgrpc::MessageChannel::GetInstance().GetOutboundChannel().push(*byteBuffer);
        }
        else if (contentCase == StreamingMessage::ContentCase::kFunctionEnvironmentReloadRequest)
        {
            try
            {
                string dir(receivedMessage.function_environment_reload_request().function_app_directory());

                {
                    funcgrpc::FuncPerfMarker mark1("Setting environment variables");

                    google::protobuf::Map<string, string> envVars =
                        receivedMessage.function_environment_reload_request().environment_variables();
                    for (auto &envVar : envVars)
                    {
                        string envString = envVar.first; // key
                        string value = envVar.second;    // value
                        envString.append("=").append(value);

                        _putenv(envString.c_str());
                    }

                    string scriptRootEnvVar("AzureWebJobsScriptRoot=" + dir);
                    _putenv(scriptRootEnvVar.c_str());
                }

                string exePath = funcgrpc::WorkerConfigHandle().GetApplicationExePath(dir);
                {
                    funcgrpc::FuncPerfMarker mark2("application_->ExecuteApplication");
                    application_->ExecuteApplication(exePath);
                }

                StreamingMessage streamingMsg;
                streamingMsg.mutable_function_environment_reload_response()->mutable_result()->set_status(
                    AzureFunctionsRpcMessages::StatusResult::Success);

                auto uPtrBb = funcgrpc::SerializeToByteBuffer(&streamingMsg);
                auto byteBuffer = uPtrBb.get();

                FUNC_LOG_DEBUG("Pushing response to outbound channel.contentCase: {}", streamingMsg.content_case());
                funcgrpc::MessageChannel::GetInstance().GetOutboundChannel().push(*byteBuffer);
                specializationRequestReceived = true;
            }
            catch (const std::exception &ex)
            {
                FUNC_LOG_ERROR("Caught unknown exception inside handler.{}", ex.what());
            }
            catch (...)
            {
                FUNC_LOG_ERROR("Caught unknown exception in handler.");
            }
        }
    }
}