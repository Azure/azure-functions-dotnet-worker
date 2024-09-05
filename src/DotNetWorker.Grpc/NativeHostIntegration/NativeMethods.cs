// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Grpc.NativeHostIntegration
{
    internal static unsafe class NativeMethods
    {
        private const string NativeWorkerDll = "FunctionsNetHost.exe";

        static NativeMethods()
        {
            NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, ImportResolver);
        }

        public static void RegisterCallbacks(
            delegate* unmanaged<byte**, int, IntPtr, IntPtr> requestCallback,
            IntPtr grpcHandler)
        {
            _ = register_callbacks(IntPtr.Zero, requestCallback, grpcHandler);
        }

        public static void SendStreamingMessage(StreamingMessage streamingMessage)
        {
            byte[] bytes = streamingMessage.ToByteArray();
            fixed (byte* ptr = bytes)
            {
                _ = send_streaming_message(IntPtr.Zero, ptr, bytes.Length);
            }
        }

        [DllImport(NativeWorkerDll)]
        private static extern int send_streaming_message(IntPtr pInProcessApplication, byte* streamingMessage, int streamingMessageSize);

        [DllImport(NativeWorkerDll)]
        private static extern unsafe int register_callbacks(IntPtr pInProcessApplication,
            delegate* unmanaged<byte**, int, IntPtr, IntPtr> requestCallback,
            IntPtr grpcHandler);

        /// <summary>
        /// Custom import resolve callback.
        /// When trying to resolve "FunctionsNetHost", we return the handle using GetMainProgramHandle API in this callback.
        /// </summary>
        private static IntPtr ImportResolver(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName == NativeWorkerDll)
            {
                return NativeLibrary.GetMainProgramHandle();
            }

            // Return 0 so that built-in resolving code will be executed.
            return IntPtr.Zero;
        }
    }
}
