// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using FunctionsNetHost.Diagnostics;

namespace FunctionsNetHost
{
    internal static class AssemblyPreloader
    {
        static private string? _basePath;
        private static List<string>? _assemblyList;

        internal static void Preload(string? applicationBasePath = null)
        {
            if (applicationBasePath != null)
            {
                _basePath = applicationBasePath;
            }

            var filePath = $"{_basePath}\\assemblies.txt";
            var assemblies = GetAssembliesToPreload(filePath);

            if (assemblies.Count == 0)
            {
                Logger.Log("No assemblies to preload");
                return;
            }

            Logger.Log($"Preloading {assemblies.Count} assemblies");

            var successfulPreloads = 0;
            var failedPreloads = 0;
            foreach (var assembly in assemblies)
            {
                var loaded = NativeLibrary.TryLoad(assembly, out _);
                Logger.Log($"Preloaded {assembly} : {loaded}");
                if (loaded)
                {
                    successfulPreloads++;
                }
                else
                {
                    failedPreloads++;
                }
            }

            AppLoaderEventSource.Log.AssembliesPreloaded(assemblies.Count, successfulPreloads, failedPreloads);
        }

        private static ICollection<string> GetAssembliesToPreload(string filePath)
        {
            if (_assemblyList != null)
            {
                Logger.Log($"Reading cached assembly list({_assemblyList.Count} items).");
                return _assemblyList;
            }

            var fileExists = File.Exists(filePath);
            Logger.Log($"File {filePath} exist:{fileExists}");

            if (File.Exists(filePath))
            {
                Logger.Log($"Reading assembly list from file: {filePath}");
                var lines = File.ReadAllLines(filePath);
                _assemblyList = new List<string>(lines);
            }
            else
            {
                _assemblyList = new List<string>();
            }

            return Array.Empty<string>();
        }
    }
}
