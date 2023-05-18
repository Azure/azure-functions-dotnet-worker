using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsNetHost
{
    public struct NativeHostData
    {
        public IntPtr pNativeApplication;
    }


    internal class NativeExports
    {

        [DllImport("FunctionsNetHost", CallingConvention = CallingConvention.Cdecl)]
        public static extern int get_application_properties(ref NativeHostData pNativeHostData);

        [DllImport("FunctionsNetHost", CallingConvention = CallingConvention.Cdecl)]
        public static extern int register_callbacks(
    ref NativeHostApplication pInProcessApplication,
    RequestHandlerDelegate requestHandler,
    IntPtr grpcHandler);

    }

    internal class InteropLayer
    {
        public static int get_application_properties(IntPtr pNativeHostData)
        {
            Logger.Log("get_application_properties was invoked");
            return 0; // Return the appropriate result
        }
    }
}
