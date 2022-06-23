// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights
{
    internal interface ISdkVersionProvider
    {
        string GetSdkVersion();
    }

    internal class FunctionsWorkerVersionProvider : ISdkVersionProvider
    {
        private readonly string sdkVersion = "azurefunctions-netiso: " + GetAssemblyFileVersion(typeof(ISdkVersionProvider).Assembly);

        public string GetSdkVersion()
        {
            return sdkVersion;
        }

        internal static string GetAssemblyFileVersion(Assembly assembly)
        {
            AssemblyFileVersionAttribute fileVersionAttr = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            return fileVersionAttr.Version;
        }
    }
}
