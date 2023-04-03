// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.Diagnostics.JitTrace;
using System.Text;
using FunctionsNetHost.ManagedLoader;
using FunctionsNetHost.ManagedLoader.NativeHostIntegration;
using System.Reflection;

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

            var appTargetFramework = GetApplicationTargetFramework();
            PreJitPrepare(appTargetFramework);

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

        private void PreJitPrepare(string targetFramework)
        {
            // This is to PreJIT all methods captured in coldstart.jittrace file to improve cold start time
            var assemblyLocalPath = Path.GetDirectoryName(new Uri(typeof(ManagedAppLoader).Assembly.Location).LocalPath);
            var filePath = Path.Combine(assemblyLocalPath!, Constants.PreJitFolderName, targetFramework, Constants.JitTraceFileName);

            Logger.Log($"JI file path: {filePath}");

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
            // As of today, we have only one message (load customer assembly) from managed to apploader.
            var span = new ReadOnlySpan<byte>(*nativeMessage, nativeMessageSize);
            var customerAssemblyPath = Encoding.UTF8.GetString(span);
            Logger.Log($"~~~ HandleAppLoaderRequest. Customer assembly path: {customerAssemblyPath} ~~~");

            // TO DO: Call the method which loads customer assembly.
            TempMethodForLoading(customerAssemblyPath);

            return IntPtr.Zero;
        }

        // Temp method I tried. Fabio will replace this.
        private static void TempMethodForLoading(string customerAssemblyPath)
        {
            Logger.Log($"~~~~  TempMethodForLoading customerAssemblyPath:{customerAssemblyPath}~~~~");

            var customerAssembly = Assembly.LoadFrom(customerAssemblyPath);
            if (customerAssembly.EntryPoint is null)
            {
                return;
            }

            var entryPointTypeInstance = Activator.CreateInstance(customerAssembly.EntryPoint.DeclaringType);
            customerAssembly.EntryPoint.Invoke(entryPointTypeInstance, new object[] { Array.Empty<string>() });

            // Tested this version and getting exception about dependencies not loaded/found.Ex: 'Microsoft.Extensions.Hosting.Abstractions.
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
