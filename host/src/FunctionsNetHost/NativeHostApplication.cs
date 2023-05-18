using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsNetHost
{
    using System;
    using System.Runtime.InteropServices;

    public delegate void RequestHandlerDelegate(ref byte buffer, int size, IntPtr handle);

    public class NativeHostApplication
    {
        private static NativeHostApplication s_Application;
        private IntPtr handle;
        private RequestHandlerDelegate callback;

        public NativeHostApplication()
        {
        }

        ~NativeHostApplication()
        {
        }

        public void HandleIncomingMessage(byte[] buffer, int size)
        {
            Logger.Log("HandleIncomingMessage");

            GCHandle bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                IntPtr bufferPtr = bufferHandle.AddrOfPinnedObject();
                
               // callback(ref bufferPtr, size, handle);
            }
            finally
            {
                bufferHandle.Free();
            }
        }

        public void SetCallbackHandles(RequestHandlerDelegate requestCallback, IntPtr grpcHandle)
        {
            Logger.Log("SetCallbackHandles");

            callback = requestCallback;
            handle = grpcHandle;
        }

        private IntPtr load_library(string path)
        {
            IntPtr h = NativeMethods.LoadLibraryW(path);
            if (h == IntPtr.Zero)
                throw new Exception("Failed to load library: " + path);
            return h;
        }

        private IntPtr get_export(IntPtr h, string name)
        {
            IntPtr f = NativeMethods.GetProcAddress(h, name);
            if (f == IntPtr.Zero)
                throw new Exception("Failed to get export: " + name);
            return f;
        }

        private static class NativeMethods
        {
            [DllImport("kernel32", CharSet = CharSet.Unicode)]
            public static extern IntPtr LoadLibraryW(string fileName);

            [DllImport("kernel32", CharSet = CharSet.Ansi)]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        }
    }

}
