// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using FunctionsNetHost.Grpc;

namespace FunctionsNetHost
{
    // If having problems with the managed host, enable the following:
    // Environment.SetEnvironmentVariable("COREHOST_TRACE", "1");
    // In Unix environment, you need to run the below command in the terminal to set the environment variable.
    // export COREHOST_TRACE=1

    /// <summary>
    /// Manages loading hostfxr & worker assembly.
    /// </summary>
    internal sealed class AppLoader : IDisposable
    {
        private IntPtr _hostfxrHandle = IntPtr.Zero;
        private IntPtr _hostContextHandle = IntPtr.Zero;
        private bool _disposed;
        private GrpcWorkerStartupOptions _workerStartupOptions;

        internal AppLoader(GrpcWorkerStartupOptions workerStartupOptions)
        {
            _workerStartupOptions = workerStartupOptions;
        }

        internal int RunApplication(string? assemblyPath)
        {
            ArgumentNullException.ThrowIfNull(assemblyPath, nameof(assemblyPath));

            unsafe
            {
                var parameters = new NetHost.get_hostfxr_parameters
                {
                    size = sizeof(NetHost.get_hostfxr_parameters),
                    assembly_path = GetCharArrayPointer(assemblyPath)
                };

                var loadStart = Stopwatch.GetTimestamp();
                var stageStart = loadStart;
                Logger.Log($"Function app load starting. Assembly path:{assemblyPath}");

                var hostfxrFullPath = NetHost.GetHostFxrPath(&parameters);
                LogFunctionAppLoadStage($"hostfxr path resolved:{hostfxrFullPath}", loadStart, stageStart);

                stageStart = Stopwatch.GetTimestamp();
                _hostfxrHandle = NativeLibrary.Load(hostfxrFullPath);

                if (_hostfxrHandle == IntPtr.Zero)
                {
                    Logger.Log($"Failed to load hostfxr. hostfxrFullPath:{hostfxrFullPath}");
                    return -1;
                }

                LogFunctionAppLoadStage("hostfxr loaded", loadStart, stageStart);

                stageStart = Stopwatch.GetTimestamp();
                var commandLineArguments = _workerStartupOptions.CommandLineArgs.Prepend(assemblyPath).ToArray();
                var error = HostFxr.Initialize(commandLineArguments.Length, commandLineArguments, IntPtr.Zero, out _hostContextHandle);

                if (_hostContextHandle == IntPtr.Zero)
                {
                    Logger.Log($"Failed to initialize the .NET Core runtime. Assembly path:{assemblyPath}");
                    return -1;
                }

                if (error < 0)
                {
                    return error;
                }

                LogFunctionAppLoadStage("hostfxr initialized", loadStart, stageStart);

                stageStart = Stopwatch.GetTimestamp();
                HostFxr.SetAppContextData(_hostContextHandle, "AZURE_FUNCTIONS_NATIVE_HOST", "1");
                LogFunctionAppLoadStage("app context data set", loadStart, stageStart);

                Logger.Log(
                    $"Function app hostfxr run starting. TotalElapsedMs:{Stopwatch.GetElapsedTime(loadStart).TotalMilliseconds:0.0}");
                var exitCode = HostFxr.Run(_hostContextHandle);
                Logger.Log(
                    $"Function app hostfxr run completed. ExitCode:{exitCode}, TotalElapsedMs:{Stopwatch.GetElapsedTime(loadStart).TotalMilliseconds:0.0}");

                return exitCode;
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
                    Logger.LogTrace($"Freed hostfxr library handle");
                    _hostfxrHandle = IntPtr.Zero;
                }

                if (_hostContextHandle != IntPtr.Zero)
                {
                    HostFxr.Close(_hostContextHandle);
                    Logger.LogTrace($"Closed hostcontext handle");
                    _hostContextHandle = IntPtr.Zero;
                }

                _disposed = true;
            }
        }

        private static unsafe char* GetCharArrayPointer(string assemblyPath)
        {
#if OS_LINUX
            return (char*)Marshal.StringToHGlobalAnsi(assemblyPath).ToPointer();
#else
            return (char*)Marshal.StringToHGlobalUni(assemblyPath).ToPointer();
#endif
        }

        private static void LogFunctionAppLoadStage(string stage, long loadStart, long stageStart)
        {
            var now = Stopwatch.GetTimestamp();
            Logger.Log(
                $"Function app load: {stage}. StepElapsedMs:{Stopwatch.GetElapsedTime(stageStart, now).TotalMilliseconds:0.0}, TotalElapsedMs:{Stopwatch.GetElapsedTime(loadStart, now).TotalMilliseconds:0.0}");
        }
    }
}
