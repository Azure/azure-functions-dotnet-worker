using System.Runtime.InteropServices;
using FunctionsNetHost.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace FunctionsNetHost
{
    public struct NativeHostData
    {
        public IntPtr pNativeApplication;
        //public CallbackDelegate Callback;
        public IntPtr Callback;
    }

    public delegate int CallbackDelegate(int value);

    public class NativeExports
    {
        

        // https://github.com/dotnet/runtime/issues/78663


        [UnmanagedCallersOnly(EntryPoint = "get_application_properties")]
        public static int get_application_properties(NativeHostData nativeHostData)
        {
            Logger.Log("get_application_properties was invoked");

            var nativeHostApplication = NativeHostApplication.Instance;

            GCHandle gch = GCHandle.Alloc(nativeHostApplication, GCHandleType.Pinned);
            IntPtr pObj = gch.AddrOfPinnedObject();
            nativeHostData.pNativeApplication = pObj;

            Logger.Log($"nativeHostApplication ptr:{pObj}");

            return 1;
        }


        [UnmanagedCallersOnly(EntryPoint = "register_callbacks")]
        public unsafe static int register_callbacks(IntPtr pInProcessApplication,
                                                delegate* unmanaged<byte**, int, IntPtr, IntPtr> requestCallback,
            IntPtr grpcHandler)
        {
            Logger.Log("register_callbacks was invoked");

            NativeHostApplication.Instance.SetCallbackHandles(requestCallback, grpcHandler);

            
            return 1;
        }

        [UnmanagedCallersOnly(EntryPoint = "send_streaming_message")]
        public unsafe static int send_streaming_message(IntPtr pInProcessApplication, byte* streamingMessage, int streamingMessageSize)
        {

            Logger.Log($"send_streaming_message was invoked. streamingMessageSize:{streamingMessageSize}");

            var span = new ReadOnlySpan<byte>(streamingMessage, streamingMessageSize);
            var outboundMessageToHost = StreamingMessage.Parser.ParseFrom(span);
           // Logger.Log($"outboundMessageToHost ContentCase: {outboundMessageToHost.ContentCase}");

            _ = MessageChannel.Instance.SendOutboundAsync(outboundMessageToHost);

            return 1;
        }
    }
}
