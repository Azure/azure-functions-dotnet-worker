// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace FunctionsNetHost.ManagedLoader.NativeHostIntegration
{
    internal static unsafe class AppLoaderNativeMethods
    {
        private const string NativeWorkerDll = "FunctionsNetHost.exe";

        public static void RegisterAppLoaderCallbacks(IntPtr pInProcessApplication,
            delegate* unmanaged<byte**, int, IntPtr, IntPtr> requestCallback,
            IntPtr grpcHandler)
        {
            _ = register_apploader_callbacks(pInProcessApplication, requestCallback, grpcHandler);
        }

        [DllImport(NativeWorkerDll)]
        private static extern unsafe int register_apploader_callbacks(IntPtr pInProcessApplication,
            delegate* unmanaged<byte**, int, IntPtr, IntPtr> requestCallback,
            IntPtr grpcHandler);
    }
}
