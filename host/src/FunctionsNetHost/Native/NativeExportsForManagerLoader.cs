// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace FunctionsNetHost
{
    public static class NativeExportsForManagerLoader
    {
        [UnmanagedCallersOnly(EntryPoint = "register_apploader_callbacks")]
        public static unsafe int RegisterAppLoaderCallbacks(IntPtr pInProcessApplication,
                                                delegate* unmanaged<byte**, int, IntPtr, IntPtr> requestCallback,
            IntPtr grpcHandler)
        {
            Logger.LogTrace("NativeExporsForManagerLoader.RegisterAppLoaderCallbacks method invoked.");

            try
            {
                NativeHostApplication.Instance.SetAppLoaderCallbackHandles(requestCallback, grpcHandler);
                return 1;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in NativeExporsForManagerLoader.RegisterCallbacks: {ex}");
                return 0;
            }
        }
    }
}
