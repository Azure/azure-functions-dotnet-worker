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

        internal int RunApplication(string? assemblyPath, string? correlationId = null)
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
                Logger.Log($"Function app load starting. CorrelationId:{correlationId}, AssemblyPath:{assemblyPath}, ProcessId:{Environment.ProcessId}");
                HostTraceManager.ScheduleDelayedFlush("function-app-load-started");

                var hostfxrFullPath = NetHost.GetHostFxrPath(&parameters);
                LogFunctionAppLoadStage($"hostfxr path resolved:{hostfxrFullPath}", correlationId, loadStart, stageStart);

                stageStart = Stopwatch.GetTimestamp();
                _hostfxrHandle = NativeLibrary.Load(hostfxrFullPath);

                if (_hostfxrHandle == IntPtr.Zero)
                {
                    LogFunctionAppLoadFailure("Failed to load hostfxr", assemblyPath, hostfxrFullPath, correlationId, loadStart);
                    return -1;
                }

                LogFunctionAppLoadStage("hostfxr loaded", correlationId, loadStart, stageStart);

                stageStart = Stopwatch.GetTimestamp();
                Logger.Log(
                    $"Function app load: command line argument construction starting. CorrelationId:{correlationId}, TotalElapsedMs:{Stopwatch.GetElapsedTime(loadStart).TotalMilliseconds:0.0}");
                var commandLineArguments = _workerStartupOptions.CommandLineArgs.Prepend(assemblyPath).ToArray();
                LogFunctionAppLoadStage($"command line arguments constructed. ArgumentCount:{commandLineArguments.Length}", correlationId, loadStart, stageStart);

                stageStart = Stopwatch.GetTimestamp();
                Logger.Log(
                    $"Function app load: HostFxr.Initialize starting. CorrelationId:{correlationId}, ArgumentCount:{commandLineArguments.Length}, TotalElapsedMs:{Stopwatch.GetElapsedTime(loadStart).TotalMilliseconds:0.0}");
                var error = HostFxr.Initialize(commandLineArguments.Length, commandLineArguments, IntPtr.Zero, out _hostContextHandle);
                LogFunctionAppLoadStage($"HostFxr.Initialize completed. ErrorCode:{error}", correlationId, loadStart, stageStart);

                if (_hostContextHandle == IntPtr.Zero)
                {
                    LogFunctionAppLoadFailure($"Failed to initialize the .NET Core runtime. ErrorCode:{error}", assemblyPath, hostfxrFullPath, correlationId, loadStart);
                    return -1;
                }

                if (error < 0)
                {
                    LogFunctionAppLoadFailure($"HostFxr.Initialize returned an error. ErrorCode:{error}", assemblyPath, hostfxrFullPath, correlationId, loadStart);
                    return error;
                }

                stageStart = Stopwatch.GetTimestamp();
                HostFxr.SetAppContextData(_hostContextHandle, "AZURE_FUNCTIONS_NATIVE_HOST", "1");
                LogFunctionAppLoadStage("app context data set", correlationId, loadStart, stageStart);

                Logger.Log(
                    $"Function app hostfxr run starting. CorrelationId:{correlationId}, TotalElapsedMs:{Stopwatch.GetElapsedTime(loadStart).TotalMilliseconds:0.0}");
                Logger.Log(
                    $"Function app load: HostFxr.Run entering managed application. CorrelationId:{correlationId}, TotalElapsedMs:{Stopwatch.GetElapsedTime(loadStart).TotalMilliseconds:0.0}");
                var exitCode = HostFxr.Run(_hostContextHandle);
                Logger.Log(
                    $"Function app hostfxr run completed. CorrelationId:{correlationId}, ExitCode:{exitCode}, TotalElapsedMs:{Stopwatch.GetElapsedTime(loadStart).TotalMilliseconds:0.0}");

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

        private static void LogFunctionAppLoadStage(string stage, string? correlationId, long loadStart, long stageStart)
        {
            var now = Stopwatch.GetTimestamp();
            Logger.Log(
                $"Function app load: {stage}. CorrelationId:{correlationId}, StepElapsedMs:{Stopwatch.GetElapsedTime(stageStart, now).TotalMilliseconds:0.0}, TotalElapsedMs:{Stopwatch.GetElapsedTime(loadStart, now).TotalMilliseconds:0.0}");
        }

        private static void LogFunctionAppLoadFailure(string failure, string assemblyPath, string? hostfxrFullPath, string? correlationId, long loadStart)
        {
            Logger.Log(
                $"Function app load failure: {failure}. CorrelationId:{correlationId}, AssemblyPath:{assemblyPath}, HostFxrPath:{hostfxrFullPath}, ProcessId:{Environment.ProcessId}, TotalElapsedMs:{Stopwatch.GetElapsedTime(loadStart).TotalMilliseconds:0.0}");
        }
    }
}
