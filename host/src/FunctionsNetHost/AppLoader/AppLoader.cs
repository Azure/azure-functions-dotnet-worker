using System.Runtime.InteropServices;

namespace FunctionsNetHost
{
    internal static class AppLoader
    {
        public static int RunApplication(string assemblyPath)
        {
            // If having problems with the managed host, enable the following:
            // Environment.SetEnvironmentVariable("COREHOST_TRACE", "1");
            // In Unix enviornment, you need to run the below command in the terminal to set the environment variable.
            // export COREHOST_TRACE=1

            Logger.Log($"Assembly path to load:{assemblyPath}");

            var hostfxrFullPath = PathResolver.GetHostFxrPath();
            IntPtr hostfxrHandle = IntPtr.Zero;

            try
            {
                hostfxrHandle = NativeLibrary.Load(hostfxrFullPath);
                if (hostfxrHandle == IntPtr.Zero)
                {
                    Logger.Log($"Failed to load hostfxr. hostfxrFullPath:{hostfxrFullPath}");
                    return -1;
                }

                Logger.Log($"hostfxr library loaded successfully.");

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
                            Logger.Log($"Failed to initialize the .NET Core runtime. dotnetBasePath:{dotnetBasePath}, hostPath:{hostPath}");
                            return -1;
                        }

                        if (error < 0)
                        {
                            return error;
                        }

                        return HostFxr.Run(host_context_handle);
                    }
                }

            }
            finally
            {
                if (hostfxrHandle != IntPtr.Zero)
                {
                    NativeLibrary.Free(hostfxrHandle);
                    Logger.Log($"Freed hostfxr library handle");
                }
            }
        }
    }
}
