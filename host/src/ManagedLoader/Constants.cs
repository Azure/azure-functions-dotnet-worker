// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.ManagedLoader
{
    public static class Constants
    {
        public const string PreJitFolderName = "PreJIT";
        public const string JitTraceFileName = "coldstart.jittrace";
        public const string OverridableAssemblyListFileName = "overridable-assemblies.txt";
    }

    public static class AppDomainProperties
    {
        public const string TrustedPlatformAssemblies = "TRUSTED_PLATFORM_ASSEMBLIES";
    }

    public static class EnvironmentVariables
    {
        public const string WorkerRuntimeVersion = "FUNCTIONS_WORKER_RUNTIME_VERSION";
    }
}
