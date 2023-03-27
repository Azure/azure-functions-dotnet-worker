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
            Logger.Log("Initializing.");
            
            var nativeHostData = NativeMethods.GetNativeHostData();
            _application = new NativeSafeHandle(nativeHostData.pNativeApplication);
            
            Logger.Log("Initialization finished.");
        }

        public unsafe void Start()
        {
            _gcHandle = GCHandle.Alloc(this);
            NativeMethods.RegisterCallbacks(_application, &HandleRequest, (IntPtr)_gcHandle);

            int ranForSeconds = 0;
            while (true)
            {
                // TEMP Heartbeat printing so we know this process is still up
                if (ranForSeconds % 5 == 0)
                {
                    Logger.Log($"ManagedAppLoader is running for last {ranForSeconds} seconds.");
                }

                Thread.Sleep(1000);
                ranForSeconds++;
            }
        }

        [UnmanagedCallersOnly]
        private static unsafe IntPtr HandleRequest(byte** nativeMessage, int nativeMessageSize, int messageType, IntPtr grpcHandler)
        {
            MessageType type = (MessageType)messageType;
            Logger.Log($"~~~ HandleRequest called. MessageType: {type} ~~~");

            switch (type) // STREAMING MESSAGE
            {
                case MessageType.StreamingMessage:
                    {
                        var span = new ReadOnlySpan<byte>(*nativeMessage, nativeMessageSize);
                        /// TO DO : Send to handler._inbound.Writer.TryWrite(msg);
                        // TO DO: Need to generate the StreamingMessage class from proto to use here.
                        break;
                    }

                case MessageType.LoadCustomerAssembly:
                    {
                        // INTERNAL REQ BETWEEN Native component and MANAGED LOADER

                        var span = new ReadOnlySpan<byte>(*nativeMessage, nativeMessageSize);
                        var assemblyPath = Encoding.UTF8.GetString(span);

                        Logger.Log($"~~~ Customer assembly path: {assemblyPath} ~~~");
                        // TO DO: Load this assembly.
                        break;
                    }
            }

            return IntPtr.Zero;
        }
    }
}
