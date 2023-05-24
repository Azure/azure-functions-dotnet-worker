// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace FunctionsNetHost
{
    internal sealed class AppLoader : IDisposable
    {
        private static readonly AppLoader _instance = new();
        private IntPtr _hostfxrHandle = IntPtr.Zero;
        private bool _disposed;

        private AppLoader()
        {
            LoadHostfxrLibrary();
        }

        internal static AppLoader Instance => _instance;

        private void LoadHostfxrLibrary()
        {
            // If having problems with the managed host, enable the following:
            //Environment.SetEnvironmentVariable("COREHOST_TRACE", "1");
            // In Unix environment, you need to run the below command in the terminal to set the environment variable.
            // export COREHOST_TRACE=1

            var hostfxrFullPath = PathResolver.GetHostFxrPath();
            Logger.LogDebug($"hostfxrFullPath:{hostfxrFullPath}");

            _hostfxrHandle = NativeLibrary.Load(hostfxrFullPath);
            if (_hostfxrHandle == IntPtr.Zero)
            {
                Logger.LogInfo($"Failed to load hostfxr. hostfxrFullPath:{hostfxrFullPath}");
                return;
            }

            Logger.LogDebug($"hostfxr library loaded successfully.");
        }

        internal int RunApplication(string assemblyPath)
        {
            Logger.LogDebug($"Assembly path to run:{assemblyPath}");
            
            unsafe
            {
                var parameters = new HostFxr.hostfxr_initialize_parameters
                {
                    size = sizeof(HostFxr.hostfxr_initialize_parameters)
                };

                var error = HostFxr.Initialize(1, new[] { assemblyPath }, ref parameters, out var hostContextHandle);

                if (hostContextHandle == IntPtr.Zero)
                {
                    Logger.LogInfo(
                        $"Failed to initialize the .NET Core runtime. assemblyPath:{assemblyPath}");
                    return -1;
                }

                if (error < 0)
                {
                    return error;
                }

                HostFxr.SetAppContextData(hostContextHandle, "AZURE_FUNCTIONS_NATIVE_HOST", "1");

                return HostFxr.Run(hostContextHandle);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (!disposing)
                {
                    return;
                }

                if (_hostfxrHandle != IntPtr.Zero)
                {
                    NativeLibrary.Free(_hostfxrHandle);
                    Logger.LogInfo($"Freed hostfxr library handle");
                    _hostfxrHandle = IntPtr.Zero;
                }

                _disposed = true;
            }
        }
    }
}
