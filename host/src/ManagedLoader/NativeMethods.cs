using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.ManagedLoader
{
    internal static unsafe partial class NativeMethods
    {
        private const string NativeWorkerDll = "FunctionsNetHost.exe";

        public static NativeHost GetNativeHostData()
        {
            _ = get_application_properties(out var hostData);
            return hostData;
        }

        public static void RegisterCallbacks(NativeSafeHandle nativeApplication,
            delegate* unmanaged<byte**, int, int, IntPtr, IntPtr> requestCallback,
            IntPtr grpcHandler)
        {
            _ = register_callbacks(nativeApplication, requestCallback, grpcHandler);
        }

        public static void RegisterMetaCallbacks(NativeSafeHandle nativeApplication,
    delegate* unmanaged<byte**, int,  int, IntPtr, IntPtr> requestCallback,
    IntPtr grpcHandler)
        {
            _ = register_callbacks(nativeApplication, requestCallback, grpcHandler);
        }

        public static void SendStreamingMessage(NativeSafeHandle nativeApplication, object streamingMessage)
        {
            //byte[] bytes = streamingMessage.ToByteArray();
            //_ = send_streaming_message(nativeApplication, bytes, bytes.Length);
        }

        [DllImport(NativeWorkerDll)]
        private static extern int get_application_properties(out NativeHost hostData);

        [DllImport(NativeWorkerDll)]
        private static extern int send_streaming_message(NativeSafeHandle pInProcessApplication, byte[] streamingMessage, int streamingMessageSize);

        [DllImport(NativeWorkerDll)]
        private static extern unsafe int register_callbacks(NativeSafeHandle pInProcessApplication,
            delegate* unmanaged<byte**, int, int, IntPtr, IntPtr> requestCallback,
            IntPtr grpcHandler);

    }
}
