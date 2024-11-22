using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FunctionsNetHost.Shared;

namespace FunctionsNetHost.PlaceholderApp.Interop
{
    internal static unsafe class NativeMethods
    {
        private const string NativeWorkerDll = "FunctionsNetHost.exe";
        private static readonly ManualResetEventSlim SpecializationWaitHandle = new(false);
        private static SpecializeMessage _specializationMessage;

        static NativeMethods()
        {
            NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, ImportResolver);
        }

        internal static unsafe void RegisterForStartupHookCallback()
        {
            RegisterStartupHookMessageHandlingCallback(&StartupHookCallbackHandler, nint.Zero);
        }

        /// <summary>
        /// Waits for the specialization message from the native code. 
        /// This method blocks until the specialization event occurs and the message is received.
        /// Once the message is received, it returns the content of the specialization message.
        /// </summary>
        internal static SpecializeMessage WaitForSpecializationMessage()
        {
            SpecializationWaitHandle.Wait();

            // Don't need _specializationWaitHandle anymore. Dispose it.
            SpecializationWaitHandle.Dispose();

            return _specializationMessage;
        }

        private static void RegisterStartupHookMessageHandlingCallback(delegate* unmanaged<byte**, int, nint, nint> requestCallback, nint grpcHandler)
        {
            _ = register_startuphook_callback(nint.Zero, requestCallback, grpcHandler);
        }

        [DllImport(NativeWorkerDll)]
        private static extern unsafe int register_startuphook_callback(nint pInProcessApplication, delegate* unmanaged<byte**, int, nint, nint> requestCallback, nint grpcHandler);

        [UnmanagedCallersOnly]
        private static unsafe nint StartupHookCallbackHandler(byte** nativeMessage, int nativeMessageSize, nint grpcHandler)
        {
            _specializationMessage = SpecializeMessage.FromByteArray(new Span<byte>(*nativeMessage, nativeMessageSize).ToArray());
            SpecializationWaitHandle.Set();

            return nint.Zero;
        }

        /// <summary>
        /// Custom import resolve callback.
        /// When trying to resolve "FunctionsNetHost", we return the handle using GetMainProgramHandle API in this callback.
        /// </summary>
        private static nint ImportResolver(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName == NativeWorkerDll)
            {
                return NativeLibrary.GetMainProgramHandle();
            }

            // Return 0 so that built-in resolving code will be executed.
            return nint.Zero;
        }
    }
}
