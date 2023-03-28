using System.Diagnostics.Metrics;
using System.Runtime.InteropServices;
using System.Text;
using FunctionsNetHost.ManagedLoader;

namespace Microsoft.Azure.Functions.Worker.ManagedLoader
{
    internal class ManagedAppLoader
    {
        private readonly NativeSafeHandle _application;
        private GCHandle _gcHandle;

        public ManagedAppLoader()
        {
            Logger.Log("Initializing...");

            var nativeHostData = NativeMethods.GetNativeHostData();
            _application = new NativeSafeHandle(nativeHostData.pNativeApplication);

            Logger.Log("Initialization finished.");
        }

        public unsafe void Start()
        {
            _gcHandle = GCHandle.Alloc(this);
            NativeMethods.RegisterAppLoaderCallbacks(_application, &HandleAppLoaderRequest, (IntPtr)_gcHandle);

            int ranForSeconds = 0;
            while (true)
            {
                // TEMP Heartbeat printing so we know this process is still up
                if (ranForSeconds % 15 == 0)
                {
                    Logger.Log($"ManagedAppLoader is running for last {ranForSeconds} seconds.");
                }

                Thread.Sleep(1000);
                ranForSeconds++;
            }
        }

        [UnmanagedCallersOnly]
        private static unsafe IntPtr HandleAppLoaderRequest(byte** nativeMessage, int nativeMessageSize, IntPtr grpcHandler)
        {
            // As of today, we have only one message (load customer assembly) from managed to apploader.
            var span = new ReadOnlySpan<byte>(*nativeMessage, nativeMessageSize);
            var customerAssemblyPath = Encoding.UTF8.GetString(span);
            Logger.Log($"~~~ HandleAppLoaderRequest. Customer assembly path: {customerAssemblyPath} ~~~");

            // TO DO: Call the method which loads customer assembly.

            return IntPtr.Zero;
        }
    }
}
