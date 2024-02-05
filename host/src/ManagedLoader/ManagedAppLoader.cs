// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.Diagnostics.JitTrace;
using System.Text;
using FunctionsNetHost.ManagedLoader;
using FunctionsNetHost.ManagedLoader.NativeHostIntegration;
using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.Azure.Functions.Worker.ManagedLoader
{
    internal class ManagedAppLoader
    {
        private readonly NativeSafeHandle _application;
        private GCHandle _gcHandle;

        public ManagedAppLoader()
        {
            Logger.Log("Initializing ManagedAppLoader");
        }

        public unsafe void StartAndWait()
        {
            _gcHandle = GCHandle.Alloc(this);

            Logger.Log("Before calling AppLoaderNativeMethods.RegisterAppLoaderCallbacks");
            AppLoaderNativeMethods.RegisterAppLoaderCallbacks(IntPtr.Zero, &HandleAppLoaderRequest, (IntPtr)_gcHandle);
            Logger.Log("After calling AppLoaderNativeMethods.RegisterAppLoaderCallbacks");

            var appTargetFramework = GetApplicationTargetFramework();
            Logger.Log($"appTafgetFramework:{appTargetFramework}");
            PreJitPrepare(appTargetFramework);

            // We want this process to not exit.
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            Logger.Log("Waiting.....");
            resetEvent.WaitOne();
        }

        private void PreJitPrepare(string targetFramework)
        {
            // This is to PreJIT all methods captured in coldstart.jittrace file to improve cold start time
            var assemblyLocalPath = Path.GetDirectoryName(new Uri(typeof(ManagedAppLoader).Assembly.Location).LocalPath);
            var filePath = Path.Combine(assemblyLocalPath!, Constants.PreJitFolderName, targetFramework, Constants.JitTraceFileName);

            var file = new FileInfo(filePath);
            var fileExist  = file.Exists;

            Logger.Log($"JIT file path: {filePath}. fileExist:{fileExist}");

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
            Logger.Log("ManagedAppLoader HandleAppLoaderRequest");

            // As of today, we have only one message (load worker assembly) from managed to apploader.
            // Native host calls this method during specialization. 
            var span = new ReadOnlySpan<byte>(*nativeMessage, nativeMessageSize);
            var workerAssemblyPath = Encoding.UTF8.GetString(span);
            Logger.Log($"ManagedAppLoader HandleAppLoaderRequest - workerAssemblyPath:{workerAssemblyPath}");
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
                Logger.Log($"~~~~  LoadWorker Invoking entry point~~~~");

                object? result = entryPoint.Invoke(null, BindingFlags.DoNotWrapExceptions, null, parameters, null);
                
                Logger.Log($"~~~~  LoadWorker Invoke result:{result}");

                if (result is not null)
                {
                    exitCode = (int)result;
                }
            }
            catch ( Exception ex )
            {
                Logger.Log($"~~~~  Error in LoadWorker:{ex}~~~~");
            }

            Logger.Log($"~~~~  LoadWorker About to call Environment.Exit");
            Environment.Exit(exitCode);
        }

        private static void ExcludeOverdidableAssemblies(string targetFramework)
        {
            var assemblyLocalPath = Path.GetDirectoryName(new Uri(typeof(ManagedAppLoader).Assembly.Location).LocalPath);
            var filePath = Path.Combine(assemblyLocalPath!, Constants.PreJitFolderName, targetFramework, Constants.OverridableAssemblyListFileName);
            Logger.Log($"Overridable assembly list file path:{filePath}");
            if (!File.Exists(filePath))
            {
                return;
            }

            string assemblies = File.ReadAllText(filePath);
            var excludeEntries = assemblies.Split(';', StringSplitOptions.RemoveEmptyEntries);
            Logger.Log($"Overridable assembly count:{excludeEntries.Length}");

            if (!excludeEntries.Any())
            {
                return;
            }

            var trustedPlatformAssembliesData = AppContext.GetData(AppDomainProperties.TrustedPlatformAssemblies);
            if (trustedPlatformAssembliesData is string trustedAssemblyListStr)
            {
                var trustedAssembliesArray = trustedAssemblyListStr.Split(';', StringSplitOptions.RemoveEmptyEntries);
                var resultArray = trustedAssembliesArray.Except(excludeEntries).ToArray();

                var newListString = string.Join(";", resultArray);
                // AppContext.SetData us available from net7.0 onwards.
                AppDomain.CurrentDomain.SetData(AppDomainProperties.TrustedPlatformAssemblies, newListString);

                var updatedTrustedPlatformAssembliesData = AppContext.GetData(AppDomainProperties.TrustedPlatformAssemblies) as string;
                var updatedTpaCount = updatedTrustedPlatformAssembliesData!.Split(';', StringSplitOptions.RemoveEmptyEntries).Length;
                Logger.Log($"Original TPA count:{trustedAssembliesArray.Length}.Updated TPA count after removing items present in the overridable assembly list file:{updatedTpaCount}");
            }
        }

        /// <summary>
        /// Gets the Target framework value of the customer function app.
        /// </summary>
        /// <returns></returns>
        private static string GetApplicationTargetFramework()
        {
            return "net8.0";
        }
    }
}
