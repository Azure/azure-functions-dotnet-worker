#include "nativehostapplication.h"
#include "func_log.h"
#include <nethost.h>

using namespace std;

NativeHostApplication *NativeHostApplication::s_Application = nullptr;

NativeHostApplication::NativeHostApplication()
{
    initMutex_ = CreateMutex(nullptr, FALSE, nullptr);
    load_hostfxr();
}

NativeHostApplication::~NativeHostApplication()
{
}

void NativeHostApplication::ExecuteApplication(string dllPath)
{
    s_Application = this;

    hostfxr_handle cxt = nullptr;

    wstring wdllPath(dllPath.begin(), dllPath.end());

    const char_t *dotnet_app = wdllPath.c_str();
    int rc = init_fptr(1, &dotnet_app, nullptr, &cxt);

    if (rc != 0 || cxt == nullptr)
    {
        std::cerr << "Init failed: " << std::hex << std::showbase << rc << '\n';
        close_fptr(cxt);
    }

    set_runtime_prop(cxt, L"AZURE_FUNCTIONS_NATIVE_HOST", L"1");

    clrThread_ = thread(
        [](hostfxr_run_app_fn r, hostfxr_handle h, HANDLE m) {
            WaitForSingleObject(m, INFINITE);

            int rc = r(h);

            if (rc != 0 || h == nullptr)
            {
                std::cerr << "Init failed2: " << std::hex << std::showbase << rc << '\n';
                // close_fptr(cxt);
            }
        },
        run_app_fptr, cxt, initMutex_);
}

void NativeHostApplication::HandleIncomingMessage(unsigned char *buffer, int size)
{
    WaitForSingleObject(initMutex_, INFINITE);

    callback(&buffer, size, handle);
}

void NativeHostApplication::SendOutgoingMessage(_In_ ByteBuffer *msg)
{
    FUNC_LOG_DEBUG("NativeHostApplication::SendOutgoingMessage > Pushing message to outbound channel.");
    auto &outboundChannel = funcgrpc::MessageChannel::GetInstance().GetOutboundChannel();
    outboundChannel.push(*msg);
}

void NativeHostApplication::SetCallbackHandles(_In_ PFN_REQUEST_HANDLER request_callback, _In_ void *grpcHandle)
{
    callback = request_callback;
    handle = grpcHandle;

    ReleaseMutex(initMutex_);
}

bool NativeHostApplication::load_hostfxr()
{
    // Pre-allocate a large buffer for the path to hostfxr
    char_t buffer[MAX_PATH];
    size_t buffer_size = sizeof(buffer) / sizeof(char_t);
    int rc = get_hostfxr_path(buffer, &buffer_size, nullptr);
    if (rc != 0)
        return false;

    // Load hostfxr and get desired exports
    void *lib = load_library(buffer);

    init_fptr =
        (hostfxr_initialize_for_dotnet_command_line_fn)get_export(lib, "hostfxr_initialize_for_dotnet_command_line");
    get_delegate_fptr = (hostfxr_get_runtime_delegate_fn)get_export(lib, "hostfxr_get_runtime_delegate");
    set_runtime_prop = (hostfxr_set_runtime_property_value_fn)get_export(lib, "hostfxr_set_runtime_property_value");
    run_app_fptr = (hostfxr_run_app_fn)get_export(lib, "hostfxr_run_app");
    close_fptr = (hostfxr_close_fn)get_export(lib, "hostfxr_close");

    return (init_fptr && get_delegate_fptr && close_fptr);
}

void *NativeHostApplication::load_library(const char_t *path)
{
    HMODULE h = ::LoadLibraryW(path);
    assert(h != nullptr);
    return (void *)h;
}

void *NativeHostApplication::get_export(void *h, const char *name)
{
    void *f = ::GetProcAddress((HMODULE)h, name);
    assert(f != nullptr);
    return f;
}