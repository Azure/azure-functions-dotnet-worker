// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace FunctionsNetHost.ManagedLoader.NativeHostIntegration
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

        // I think we can remove the same type we added to src/DotNetWorker.Grpc/NativeHostIntegration 
        [DllImport(NativeWorkerDll)]
        private static extern int get_application_properties(out NativeHost hostData);

        [DllImport(NativeWorkerDll)]
        private static extern unsafe int register_apploader_callbacks(NativeSafeHandle pInProcessApplication,
            delegate* unmanaged<byte**, int, IntPtr, IntPtr> requestCallback,
            IntPtr grpcHandler);
    }
}
