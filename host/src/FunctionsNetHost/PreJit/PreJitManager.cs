// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace FunctionsNetHost.Prejit
{
    internal class PreJitManager
    {
        private const string PlaceholderAppDirectory = "PlaceholderApp";
        private const string PlaceholderAppAssemblyName = "FunctionsNetHost.PlaceholderApp.dll";
        private const string JitTraceDirectory = "JitTrace";
#if OS_LINUX
        private const string JitTraceFileName = "linux.coldstart.jittrace";
#else
        private const string JitTraceFileName = "coldstart.jittrace";
#endif

        /// <summary>
        /// Starts the placeholder app for the current runtime version.
        /// Startup hook code is part of this placeholder app.
        /// </summary>
        internal static void InitializeAndRunPreJitPlaceholderApp(NetHostRunOptions applicationRunOption, AppLoader appLoader)
        {

            var placeHolderAppDir = Path.Combine(applicationRunOption.ExecutableDirectory, PlaceholderAppDirectory, applicationRunOption.RuntimeVersion);
            var placeholderAppAssemblyPath = Path.Combine(placeHolderAppDir, PlaceholderAppAssemblyName);
            if (!File.Exists(placeholderAppAssemblyPath))
            {
                throw new FileNotFoundException($"Placeholder app assembly not found at the specified path:{placeholderAppAssemblyPath}");
            }

            var preJitFilePath = Path.Combine(placeHolderAppDir, JitTraceDirectory, JitTraceFileName);
            if (!File.Exists(preJitFilePath))
            {
                throw new FileNotFoundException($"Pre-jit file not found at the specified path:{preJitFilePath}");
            }

            EnvironmentUtils.SetValue(Shared.EnvironmentVariables.PreJitFilePath, preJitFilePath);
            EnvironmentUtils.SetValue(Shared.EnvironmentVariables.DotnetStartupHooks, placeholderAppAssemblyPath);

            Logger.Log($"Going to run placeholder app:{placeholderAppAssemblyPath}");
            _ = Task.Run(() => appLoader.RunApplication(placeholderAppAssemblyPath));
        }
    }
}
