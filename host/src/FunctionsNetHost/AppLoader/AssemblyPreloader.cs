// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using FunctionsNetHost.Diagnostics;

namespace FunctionsNetHost
{
    internal static class AssemblyPreloader
    {
        private const string _preloadAssemblyListFile = "assemblies.txt";
        private static string? _basePath;

        internal static void Preload(string? applicationBasePath = null)
        {
            if (applicationBasePath != null)
            {
                _basePath = applicationBasePath;
            }

            var filePath = Path.Combine(_basePath!, _preloadAssemblyListFile);
            var assemblies = GetAssembliesToPreload(filePath);

            if (assemblies.Count == 0)
            {
                Logger.Log("No assemblies to preload");
                return;
            }

            Logger.Log($"Preloading {assemblies.Count} assemblies");

            var successfulPreloadCount = 0;
            var failedPreloadCount = 0;
            foreach (var assembly in assemblies)
            {
                var loaded = NativeLibrary.TryLoad(assembly, out _);
                Logger.Log($"Preloaded {assembly} : {loaded}");
                if (loaded)
                {
                    successfulPreloadCount++;
                }
                else
                {
                    failedPreloadCount++;
                }
            }
            Logger.Log($"Assembly preload summary: {successfulPreloadCount} out of {assemblies.Count} assemblies preloaded");
            AppLoaderEventSource.Log.AssembliesPreloaded(assemblies.Count, successfulPreloadCount, failedPreloadCount);
        }

        private static ICollection<string> GetAssembliesToPreload(string filePath)
        {
            var fileExists = File.Exists(filePath);
            Logger.Log($"File {filePath} exist:{fileExists}");

            if (File.Exists(filePath))
            {
                Logger.Log($"Reading assembly list from file: {filePath}");
                var lines = File.ReadAllLines(filePath);
                return new List<string>(lines.Where(line=>string.IsNullOrWhiteSpace(line) == false));
            }
            else
            {
                return new List<string>();
            }
        }
    }
}
