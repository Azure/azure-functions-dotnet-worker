// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using FunctionsNetHost.Grpc;

namespace FunctionsNetHost
{
    /// <summary>
    /// Encapsulates various configuration options required to run the FunctionsNetHost application.
    /// </summary>
    internal sealed class NetHostRunOptions
    {
        //.NET 8.0 is the minimum version that supports pre-jitting.
        private const int MinimumNetTfmToSupportPreJit = 8;

        /// <summary>
        /// Gets a value indicating whether pre-jitting is supported.
        /// </summary>
        public bool IsPreJitSupported { get; }

        /// <summary>
        /// Gets the worker startup options.
        /// </summary>
        public GrpcWorkerStartupOptions WorkerStartupOptions { get; }

        /// <summary>
        /// Gets the runtime version. This usually corresponds to the .NET runtime version.
        /// Example value: 8.0.
        /// </summary>
        public string RuntimeVersion { get; }

        /// <summary>
        /// Gets the directory where the FunctionsNetHost executable is located.
        /// </summary>
        public string ExecutableDirectory { get; }

        public NetHostRunOptions(GrpcWorkerStartupOptions workerStartupOptions, string executableDirectory)
        {
            WorkerStartupOptions = workerStartupOptions;
            ExecutableDirectory = executableDirectory;
            RuntimeVersion = EnvironmentUtils.GetValue(EnvironmentVariables.FunctionsWorkerRuntimeVersion)!;
            IsPreJitSupported = IsPrejitSupported(RuntimeVersion);
        }

        private static bool IsPrejitSupported(string runtimeVersion)
        {
            if (string.IsNullOrEmpty(runtimeVersion))
            {
                return false;
            }

            var disablePrejitEnvironmentVaValue = EnvironmentUtils.GetValue(EnvironmentVariables.DisablePrejit);
            if (string.Equals(disablePrejitEnvironmentVaValue, "1"))
            {
                Logger.Log($"PreJitting is disabled due to the environment variable '{EnvironmentVariables.DisablePrejit}' being set to '{disablePrejitEnvironmentVaValue}'.");
                return false;
            }

            return decimal.TryParse(runtimeVersion, out var value) && value >= MinimumNetTfmToSupportPreJit;
        }
    }
}

