// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace FunctionsNetHost
{
    public sealed class NativeHostApplication
    {
        private IntPtr _workerHandle;
        unsafe delegate* unmanaged<byte**, int, IntPtr, IntPtr> _requestHandlerCallback;
        unsafe delegate* unmanaged<byte**, int, IntPtr, IntPtr> _appLoaderRequestHandlerCallback;

        public static NativeHostApplication Instance { get; } = new();

        private NativeHostApplication()
        {
        }

        public unsafe void HandleInboundMessage(byte[] buffer, int size)
        {
            fixed (byte* pBuffer = buffer)
            {
                _requestHandlerCallback(&pBuffer, size, _workerHandle);
            }
        }

        public unsafe void SendToAppLoader(byte[] buffer, int size)
        {
            fixed (byte* pBuffer = buffer)
            {
                _appLoaderRequestHandlerCallback(&pBuffer, size, _workerHandle);
            }
        }

        public unsafe void SetAppLoaderCallbackHandles(delegate* unmanaged<byte**, int, IntPtr, IntPtr> callback, IntPtr grpcHandle)
        {
            _appLoaderRequestHandlerCallback = callback;
        }

        public unsafe void SetCallbackHandles(delegate* unmanaged<byte**, int, IntPtr, IntPtr> callback, IntPtr grpcHandle)
        {
            _requestHandlerCallback = callback;
            _workerHandle = grpcHandle;

            WorkerLoadStatusSignalManager.Instance.Signal.Set();
        }
    }
}
