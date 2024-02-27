// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using FunctionsNetHost.Diagnostics;

namespace FunctionsNetHost
{
    internal static class AssemblyPreloader
    {
        private static string _preloadAssemblyListFile = string.Empty;
        private static string? _basePath;

        internal static string? GetMaxDotNetVersionDirName()
        {
            // Determine the base path depending on the operating system
            string basePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                : "/usr/share/dotnet/shared/Microsoft.AspNetCore.App";

            string path = Path.Combine(basePath, "dotnet", "shared", "Microsoft.AspNetCore.App");

            if (Directory.Exists(path))
            {
                var topVersion = Directory.EnumerateDirectories(path)
                                .Select(d => new DirectoryInfo(d).Name)
                                .OrderByDescending(d => d).FirstOrDefault();

                return topVersion;
            }

            return null;
        }

        internal static void Preload(string? applicationBasePath = null)
        {
            if (applicationBasePath != null)
            {
                _basePath = applicationBasePath;
            }
            

            if (OperatingSystem.IsWindows())
            {
                _preloadAssemblyListFile = "assemblies-win.txt";
            }
            else
            {
                _preloadAssemblyListFile = "assemblies-linux.txt";
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

            ReadRuntimeAssemblyFiles(assemblies);
        }

        private static ICollection<string> GetAssembliesToPreload(string filePath)
        {
            var resultList = new List<string>();
            var fileExists = File.Exists(filePath);
            Logger.Log($"Preload assembly list file {filePath} exist:{fileExists}");

            if (fileExists)
            {
                var maxDotNetVersionDirName = GetMaxDotNetVersionDirName();
                var lines = File.ReadAllLines(filePath);

                foreach (var line in lines.Where(l => string.IsNullOrWhiteSpace(l) == false))
                {
                    var assemblyPath = line;
                    if (maxDotNetVersionDirName != null)
                    {
                        assemblyPath = line.Replace("8.0.1", maxDotNetVersionDirName);
                    }

                    var fileExist = File.Exists(assemblyPath);
                    Logger.Log($"Preload assembly {assemblyPath} exist:{fileExist}");

                    var assemblyPathNew = Path.GetFullPath(assemblyPath);
                    var newFileExist = File.Exists(assemblyPathNew);
                    Logger.Log($"Preload assembly {assemblyPathNew} exist:{newFileExist}");

                    resultList.Add(assemblyPathNew);

                }
            }



            return resultList;
        }

        internal static void ReadRuntimeAssemblyFiles(ICollection<string> assemblies)
        {
            int readCounter = 0;
            try
            {
                // Read File content in 4K chunks
                int maxBuffer = 4 * 1024;
                byte[] chunk = new byte[maxBuffer];
                Random random = new Random();
                foreach (string file in assemblies)
                {
                    // Read file content to avoid disk reads during specialization. This is only to page-in bytes.
                    ReadFileInChunks(file, chunk, maxBuffer, random);
                    readCounter++;
                }

            }
            catch (Exception ex)
            {
                Logger.Log($"Error in ReadRuntimeAssemblyFiles. {ex}");
            }
            finally
            {
                Logger.Log($"ReadRuntimeAssemblyFiles summary. Number of files read:{readCounter}");
            }
        }

        private static void ReadFileInChunks(string file, byte[] chunk, int maxBuffer, Random random)
        {
            try
            {
                using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    int bytesRead;
                    while ((bytesRead = fileStream.Read(chunk, 0, maxBuffer)) != 0)
                    {
                        // Read one random byte for every 4K bytes - 4K is default OS page size. This will help avoid disk read during specialization
                        // see for details on OS page buffering in Windows - https://docs.microsoft.com/en-us/windows/win32/fileio/file-buffering
                        var randomByte = Convert.ToInt32(chunk[random.Next(0, bytesRead - 1)]);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading file '{file}'.{ex}");
            }
        }
    }
}
