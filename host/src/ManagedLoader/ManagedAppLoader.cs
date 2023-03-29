// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Metrics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.JitTrace;
using System.Text;
using FunctionsNetHost.ManagedLoader;
using FunctionsNetHost.ManagedLoader.NativeHostIntegration;

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

            PreJitPrepare(Constants.JitTraceFileName);

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

        private void PreJitPrepare(string jitTraceFileName)
        {
            // This is to PreJIT all methods captured in coldstart.jittrace file to improve cold start time
            var assemblyLocalPath = Path.GetDirectoryName(new Uri(typeof(ManagedAppLoader).Assembly.Location).LocalPath);
            Logger.Log($"JI file path: {assemblyLocalPath}");

            var path = Path.Combine(assemblyLocalPath!, Constants.PreJitFolderName, jitTraceFileName);

            var file = new FileInfo(path);

            if (file.Exists)
            {
                JitTraceRuntime.Prepare(file, out int successfulPrepares, out int failedPrepares);

                // We will need to monitor failed vs success prepares and if the failures increase, it means code paths have diverged or there have been updates on dotnet core side.
                // When this happens, we will need to regenerate the coldstart.jittrace file.
                Logger.Log(
                    $"PreJIT Successful prepares: {successfulPrepares}, Failed prepares: {failedPrepares} FileName = {jitTraceFileName}");
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
