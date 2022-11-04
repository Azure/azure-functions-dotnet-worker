// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Grpc.NativeHostIntegration
{
    internal static unsafe partial class NativeMethods
    {
        internal const string NativeWorkerDll = "FunctionsNetHost.exe";

        public static NativeHost GetNativeHostData()
        {
            _ = get_application_properties(out var hostData);
            return hostData;
        }

        public static void RegisterCallbacks(NativeSafeHandle nativeApplication,
            delegate* unmanaged<byte**, int, IntPtr, IntPtr> requestCallback,
            IntPtr grpcHandler)
        {
            _ = register_callbacks(nativeApplication, requestCallback, grpcHandler);
        }

        public static void SendStreamingMessage(NativeSafeHandle nativeApplication, StreamingMessage streamingMessage)
        {
            byte[] bytes = streamingMessage.ToByteArray();
            _ = send_streaming_message(nativeApplication, bytes, bytes.Length);
        }

        [DllImport(NativeWorkerDll)]
        private static extern int get_application_properties(out NativeHost hostData);

        [DllImport(NativeWorkerDll)]
        private static extern int send_streaming_message(NativeSafeHandle pInProcessApplication, byte[] streamingMessage, int streamingMessageSize);

        [DllImport(NativeWorkerDll)]
        private static extern unsafe int register_callbacks(NativeSafeHandle pInProcessApplication,
            delegate* unmanaged<byte**, int, IntPtr, IntPtr> requestCallback,
            IntPtr grpcHandler);
    }
}
