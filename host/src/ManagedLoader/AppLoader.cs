using System.Runtime.InteropServices;

namespace ManagedLoader
{
    internal class ManagedAppLoader
    {
        private readonly NativeSafeHandle _application;
        private GCHandle _gcHandle;

        public ManagedAppLoader()
        {
            Console.WriteLine($"~~~ ManagedAppLoader initialized ~~~");
            var nativeHostData = NativeMethods.GetNativeHostData();
            _application = new NativeSafeHandle(nativeHostData.pNativeApplication);
        }

        public unsafe void Start()
        {
            _gcHandle = GCHandle.Alloc(this);
            NativeMethods.RegisterCallbacks(_application, &HandleRequest, (IntPtr)_gcHandle);

            int ranForSeconds = 0;
            while (true)
            {
                Console.WriteLine($"Program still running for last {ranForSeconds++} seconds.");
                Thread.Sleep(1000);
            }
        }

        [UnmanagedCallersOnly]
        private static unsafe IntPtr HandleRequest(byte** nativeMessage, int nativeMessageSize, int messageType, IntPtr grpcHandler)
        {
            MessageType type = (MessageType)messageType;
            Console.WriteLine($"~~~ HandleRequest called. MessageType: {type} ~~~");

            switch (type) // STREAMING MESSAGE
            {
                case MessageType.StreamingMessage:
                    {
                        var span = new ReadOnlySpan<byte>(*nativeMessage, nativeMessageSize);
                        /// TO DO : Send to handler._inbound.Writer.TryWrite(msg);
                        // Need to geneate the StreamingMessage class from proto to use here.
                        break;
                    }

                case MessageType.LoadCustomerAssembly:
                    {
                        // INTERNAL REQ BETWEEN MANADED COMPONENT and MANAGED LOADER
                        // TO DO : Load customer assembly.
                        var span = new ReadOnlySpan<byte>(*nativeMessage, nativeMessageSize);
                        var assemblyPath = "to do: read from span"; //span.ToString();
                        Console.WriteLine($"~~~ Customer assembly: {assemblyPath} ~~~");
                        break;
                    }
            }

            return IntPtr.Zero;
        }
    }
}
