#include "../funcgrpc/byte_buffer_helper.h"
#include "../funcgrpc/func_log.h"
#include "../funcgrpc/nativehostapplication.h"
#include <cstring>
#include <windows.h>
struct NativeHostData
{
    NativeHostApplication *pNativeApplication;
};

extern "C" __declspec(dllexport) HRESULT get_application_properties(_In_ NativeHostData *pNativeHostData)
{
    auto pInProcessApplication = NativeHostApplication::GetInstance();

    if (pInProcessApplication == nullptr)
    {
        return E_FAIL;
    }

    pNativeHostData->pNativeApplication = pInProcessApplication;

    return S_OK;
}

extern "C" __declspec(dllexport) HRESULT send_streaming_message(_In_ NativeHostApplication *pInProcessApplication,
                                                                _In_ char *managedMessage, _In_ int managedMessageSize)
{
    FUNC_LOG_DEBUG("Calling send_streaming_message. managedMessageSize:{}", managedMessageSize);

    if (managedMessageSize == 0)
    {
        FUNC_LOG_WARN("send_streaming_message. size 0");
        return S_OK;
    }

    auto bbUPtr = funcgrpc::SerializeToByteBufferFromChar(managedMessage, managedMessageSize);
    auto byteBuffer = bbUPtr.get();
    pInProcessApplication->SendOutgoingMessage(byteBuffer);

    return S_OK;
}

extern "C" __declspec(dllexport) HRESULT
    register_callbacks(_In_ NativeHostApplication *pInProcessApplication, _In_ PFN_REQUEST_HANDLER request_handler,
                       _In_ VOID *grpcHandler)
{
    if (pInProcessApplication == nullptr)
    {
        return E_INVALIDARG;
    }

    pInProcessApplication->SetCallbackHandles(request_handler, grpcHandler);

    return S_OK;
}