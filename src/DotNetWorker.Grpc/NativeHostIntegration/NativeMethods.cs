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

        // NET7.0 onwards, we use custom import resolver to get our native executable handle.
        // which works for both Windows and Linux.
#if NET7_0_OR_GREATER
        static NativeMethods()
        {
            NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, ImportResolver);
        }
#endif

        public static NativeHost GetNativeHostData()
        {
            var result = get_application_properties(out var hostData);
            if (result == 1)
            {
                return hostData;
            }

            throw new InvalidOperationException($"Invalid result returned from get_application_properties: {result}");
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

        [DllImport(NativeWorkerDll, CharSet = CharSet.Auto)]
        private static extern int get_application_properties(out NativeHost hostData);

        [DllImport(NativeWorkerDll, CharSet = CharSet.Auto)]
        private static extern int send_streaming_message(NativeSafeHandle pInProcessApplication, byte[] streamingMessage, int streamingMessageSize);

        [DllImport(NativeWorkerDll, CharSet = CharSet.Auto)]
        private static extern unsafe int register_callbacks(NativeSafeHandle pInProcessApplication,
            delegate* unmanaged<byte**, int, IntPtr, IntPtr> requestCallback,
            IntPtr grpcHandler);

#if NET7_0_OR_GREATER
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
#endif
    }
}
