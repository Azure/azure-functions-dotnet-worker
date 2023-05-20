using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsNetHost
{
    public unsafe delegate void RequestHandlerDelegate(byte** buffer, int size, IntPtr handle);

    public class NativeHostApplication
    {
        static readonly NativeHostApplication instance = new NativeHostApplication();

        static NativeHostApplication()
        {
        }

        NativeHostApplication()
        {
        }

        public static NativeHostApplication Instance
        {
            get
            {
                return instance;
            }
        }

        private IntPtr handle;
        unsafe delegate* unmanaged<byte**, int, IntPtr, IntPtr> requestHandlerCallback;




        public unsafe void HandleInboundMessage(byte[] buffer, int size)
        {
            Logger.Log($"HandleInboundMessage. length:{size}");

            GCHandle bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                IntPtr bufferPtr = bufferHandle.AddrOfPinnedObject();

                requestHandlerCallback((byte**)&bufferPtr, size, handle);
                // requestHandlerCallback(buffer, size, handle);
            }
            finally
            {
                bufferHandle.Free();
            }
        }

        public unsafe void SetCallbackHandles(delegate* unmanaged<byte**, int, IntPtr, IntPtr> callback, IntPtr grpcHandle)
        {
            Logger.Log("NativeApplications.SetCallbackHandles invoked");

            requestHandlerCallback = callback;
            handle = grpcHandle;
        }
    }

}
