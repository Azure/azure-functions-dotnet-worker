#pragma once
#include "messaging_channel.h"
#include <FunctionRpc.pb.h>
#include <coreclr_delegates.h>
#include <fstream>
#include <hostfxr.h>
#include <iostream>
#include <nethost.h>
#include <string>
#include <windows.h>

#include <grpc/grpc.h>
#include <grpcpp/channel.h>
#include <grpc/byte_buffer.h>

using grpc::ByteBuffer;
using namespace std;

// delegate for requests
typedef int(__stdcall *PFN_REQUEST_HANDLER)(unsigned char **msg, int size, void *grpcHandle);

class NativeHostApplication
{
  public:
    NativeHostApplication();

    ~NativeHostApplication();

    void ExecuteApplication(string dllPath);

    void SetCallbackHandles(_In_ PFN_REQUEST_HANDLER request_callback, _In_ void *grpcHandle);

    void HandleIncomingMessage(_In_ unsigned char *buffer, _In_ int size);

    void SendOutgoingMessage(_In_ ByteBuffer *message);

    static NativeHostApplication *GetInstance()
    {
        return s_Application;
    }

  private:
    static NativeHostApplication *s_Application;

    // Globals to hold hostfxr exports
    hostfxr_initialize_for_dotnet_command_line_fn init_fptr;
    hostfxr_get_runtime_delegate_fn get_delegate_fptr;
    hostfxr_set_runtime_property_value_fn set_runtime_prop;
    hostfxr_run_app_fn run_app_fptr;
    hostfxr_close_fn close_fptr;

    bool load_hostfxr();
    void *load_library(const char_t *);
    void *get_export(void *h, const char *name);

    PFN_REQUEST_HANDLER callback;
    void *handle;

    thread clrThread_;
    HANDLE initMutex_;
};