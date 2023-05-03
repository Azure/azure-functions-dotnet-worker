// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.Diagnostics.JitTrace;
using System.Text;
using FunctionsNetHost.ManagedLoader;
using FunctionsNetHost.ManagedLoader.NativeHostIntegration;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Loader;

namespace Microsoft.Azure.Functions.Worker.ManagedLoader
{
    internal class ManagedAppLoader
    {
        private readonly NativeSafeHandle _application;
        private GCHandle _gcHandle;

        public ManagedAppLoader()
        {
            Logger.Log("Initializing...");

            var nativeHostData = AppLoaderNativeMethods.GetNativeHostData();
            _application = new NativeSafeHandle(nativeHostData.pNativeApplication);

            Logger.Log("Initialization finished.");
        }

        public unsafe void StartAndWait()
        {
            _gcHandle = GCHandle.Alloc(this);
            AppLoaderNativeMethods.RegisterAppLoaderCallbacks(_application, &HandleAppLoaderRequest, (IntPtr)_gcHandle);

            var appTargetFramework = GetApplicationTargetFramework();
            PreJitPrepare(appTargetFramework);

            // We want this process to not exit.
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            resetEvent.WaitOne();
        }

        private void PreJitPrepare(string targetFramework)
        {
            // This is to PreJIT all methods captured in coldstart.jittrace file to improve cold start time
            var assemblyLocalPath = Path.GetDirectoryName(new Uri(typeof(ManagedAppLoader).Assembly.Location).LocalPath);
            var filePath = Path.Combine(assemblyLocalPath!, Constants.PreJitFolderName, targetFramework, Constants.JitTraceFileName);

            Logger.Log($"JIT file path: {filePath}");

            var file = new FileInfo(filePath);

            if (!file.Exists)
            {
                return;
            }

            JitTraceRuntime.Prepare(file, out int successfulPrepares, out int failedPrepares);

            // We will need to monitor failed vs success prepares and if the failures increase, it means code paths have diverged or there have been updates on dotnet core side.
            // When this happens, we will need to regenerate the coldstart.jittrace file.
            Logger.Log(
                $"PreJIT Successful prepares: {successfulPrepares}, Failed prepares: {failedPrepares} FileName = {targetFramework}/{Constants.JitTraceFileName}");
        }

        [UnmanagedCallersOnly]
        private static unsafe IntPtr HandleAppLoaderRequest(byte** nativeMessage, int nativeMessageSize, IntPtr grpcHandler)
        {
            // As of today, we have only one message (load worker assembly) from managed to apploader.
            // Native host calls this method during specialization. 
            var span = new ReadOnlySpan<byte>(*nativeMessage, nativeMessageSize);
            var workerAssemblyPath = Encoding.UTF8.GetString(span);
            //Logger.Log($"~~~ HandleAppLoaderRequest. Worker assembly path: {workerAssemblyPath} ~~~");

            _ = Task.Run(() => LoadWorker(workerAssemblyPath));

            return IntPtr.Zero;
        }

        private static void LoadWorker(string workerAssemblyPath)
        {
            Logger.Log($"~~~~  LoadWorker workerAssemblyPath:{workerAssemblyPath}~~~~");

            // Initialize the assembly resolver to ensure we can load worker dependencies
            WorkerAssemblyResolver.Initialize(AssemblyLoadContext.Default, workerAssemblyPath);

            Assembly customerAssembly = Assembly.LoadFrom(workerAssemblyPath);
            MethodInfo? entryPoint = customerAssembly.EntryPoint 
                ?? throw new MissingMethodException($"Assembly ('{customerAssembly.FullName}') missing entry point.");

            var parameters = entryPoint.GetParameters().Length > 0 ? new object[] { Environment.GetCommandLineArgs() } : null;

            int exitCode = 0;

            try
            {
                object? result = entryPoint.Invoke(null, BindingFlags.DoNotWrapExceptions, null, parameters, null);

                if (result is not null)
                {
                    exitCode = (int)result;
                }
            }
            catch ( Exception ex )
            {
                Logger.Log($"~~~~  Error in LoadWorker:{ex}~~~~");
            }

            Environment.Exit(exitCode);
        }

        /// <summary>
        /// Gets the Target framework value of the customer function app.
        /// </summary>
        /// <returns></returns>
        private static string GetApplicationTargetFramework()
        {
            // TO DO : Read from what managed code is passing.
            // May be read from AppContext.GetData or Environment variable or read from cmdline args?
            // var applicationTfm = AppContext.GetData("AZURE_FUNCTIONS_ISOLATED_APP_TFM");

            return "net6.0";
        }
    }
}
