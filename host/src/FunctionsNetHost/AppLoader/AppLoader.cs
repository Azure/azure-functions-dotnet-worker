using FunctionsNetHost;
using System.Runtime.InteropServices;

namespace FunctionsNetHost
{
    internal sealed class AppLoader : IDisposable
    {
        private static readonly AppLoader instance = new();
        private IntPtr hostfxrHandle = IntPtr.Zero;
        private bool disposed;

        private AppLoader()
        {
            LoadHostfxrLibrary();
        }

        internal static AppLoader Instance => instance;

        private void LoadHostfxrLibrary()
        {
            // If having problems with the managed host, enable the following:
            // Environment.SetEnvironmentVariable("COREHOST_TRACE", "1");
            // In Unix environment, you need to run the below command in the terminal to set the environment variable.
            // export COREHOST_TRACE=1

            var hostfxrFullPath = PathResolver.GetHostFxrPath();
            hostfxrHandle = NativeLibrary.Load(hostfxrFullPath);
            if (hostfxrHandle == IntPtr.Zero)
            {
                Logger.Log($"Failed to load hostfxr. hostfxrFullPath:{hostfxrFullPath}");
                throw new Exception("Failed to load hostfxr.");
            }

            Logger.Log($"hostfxr library loaded successfully.");
        }

        internal int RunApplication(string assemblyPath)
        {
            Logger.Log($"Assembly path to load: {assemblyPath}");

            var hostPath = Environment.CurrentDirectory;
            var dotnetBasePath = PathResolver.GetDotnetRootPath();

            unsafe
            {
                fixed (char* hostPathPointer = hostPath)
                fixed (char* dotnetRootPointer = dotnetBasePath)
                {
                    var parameters = new HostFxr.hostfxr_initialize_parameters
                    {
                        size = sizeof(HostFxr.hostfxr_initialize_parameters)
                    };

                    var error = HostFxr.Initialize(1, new string[] { assemblyPath }, ref parameters, out var host_context_handle);

                    if (host_context_handle == IntPtr.Zero)
                    {
                        Logger.Log($"Failed to initialize the .NET Core runtime. dotnetBasePath:{dotnetBasePath}, hostPath:{hostPath}, assemblyPath:{assemblyPath}");
                        return -1;
                    }

                    if (error < 0)
                    {
                        return error;
                    }

                    HostFxr.SetAppContextData(host_context_handle, "AZURE_FUNCTIONS_NATIVE_HOST", "1");

                    return HostFxr.Run(host_context_handle);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (!disposing)
                {
                    return;
                }

                if (hostfxrHandle != IntPtr.Zero)
                {
                    NativeLibrary.Free(hostfxrHandle);
                    Logger.Log($"Freed hostfxr library handle");
                    hostfxrHandle = IntPtr.Zero;
                }

                disposed = true;
            }
        }
    }
}
