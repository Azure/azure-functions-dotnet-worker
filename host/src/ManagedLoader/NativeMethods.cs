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

        public static void RegisterAppLoaderCallbacks(NativeSafeHandle nativeApplication, 
            delegate* unmanaged<byte**, int, IntPtr, IntPtr> requestCallback,
            IntPtr grpcHandler)
        {
            _ = register_apploader_callbacks(nativeApplication, requestCallback, grpcHandler);
        }

        [DllImport(NativeWorkerDll)]
        private static extern int get_application_properties(out NativeHost hostData);

        [DllImport(NativeWorkerDll)]
        private static extern unsafe int register_apploader_callbacks(NativeSafeHandle pInProcessApplication,
            delegate* unmanaged<byte**, int, IntPtr, IntPtr> requestCallback,
            IntPtr grpcHandler);
    }
}
