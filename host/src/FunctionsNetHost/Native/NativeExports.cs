// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace FunctionsNetHost
{
    public static class NativeExports
    {
        [UnmanagedCallersOnly(EntryPoint = "get_application_properties")]
        public static int GetApplicationProperties(NativeHostData nativeHostData)
        {
            Logger.LogTrace("NativeExports.GetApplicationProperties method invoked.");

            try
            {
                var nativeHostApplication = NativeHostApplication.Instance;
                GCHandle gch = GCHandle.Alloc(nativeHostApplication, GCHandleType.Pinned);
                IntPtr pNativeApplication = gch.AddrOfPinnedObject();
                nativeHostData.PNativeApplication = pNativeApplication;

                return 1;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in NativeExports.GetApplicationProperties: {ex}");
                return 0;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "register_callbacks")]
        public static unsafe int RegisterCallbacks(IntPtr pInProcessApplication,
            delegate* unmanaged<byte**, int, IntPtr, IntPtr> requestCallback,
            IntPtr grpcHandler)
        {
            Logger.LogTrace("NativeExports.RegisterCallbacks method invoked.");

            try
            {
                var nativeHostApplication = GCHandle.FromIntPtr(pInProcessApplication).Target as NativeHostApplication;
                nativeHostApplication!.SetCallbackHandles(requestCallback, grpcHandler);

                return 1;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in RegisterCallbacks: {ex}");
                return 0;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "send_streaming_message")]
        public static unsafe int SendStreamingMessage(IntPtr pInProcessApplication, byte* streamingMessage,
            int streamingMessageSize)
        {
            Logger.LogTrace($"NativeExports.SendStreamingMessage method invoked.");

            try
            {
                var nativeHostApplication = GCHandle.FromIntPtr(pInProcessApplication).Target as NativeHostApplication;
                nativeHostApplication!.SendOutgoingMessage(streamingMessage, streamingMessageSize);

                return 1;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in SendStreamingMessage: {ex}");
                return 0;
            }
        }
    }
}
