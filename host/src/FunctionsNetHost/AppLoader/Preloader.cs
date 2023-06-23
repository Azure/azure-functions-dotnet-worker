// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace FunctionsNetHost
{
    internal static class Preloader
    {
        internal static void ReadFiles()
        {
            const string netBasePath = @"C:\Program Files (x86)\dotnet\shared\Microsoft.NETCore.App\6.0.14\";
            const string aspBasePath = @"C:\Program Files (x86)\dotnet\shared\Microsoft.AspNetCore.App\6.0.14\";

            string[] arr = new[]
            {
                $@"{netBasePath}System.Collections.Concurrent.dll",
                $@"{netBasePath}coreclr.dll",
                $@"{netBasePath}System.Collections.dll",
                $@"{netBasePath}System.Collections.Immutable.dll",
                $@"{netBasePath}System.Diagnostics.Tracing.dll",
                $@"{netBasePath}System.Linq.dll",
                $@"{netBasePath}System.Net.Http.dll",
                $@"{netBasePath}System.Private.CoreLib.dll",
                $@"{netBasePath}System.Runtime.dll",
                $@"{netBasePath}System.Text.Json.dll",
                $@"{netBasePath}System.Threading.Channels.dll",
                $@"{netBasePath}System.Threading.dll",
                $@"{netBasePath}System.Threading.Thread.dll",
                $@"{aspBasePath}Microsoft.Extensions.Hosting.dll",
                $@"{aspBasePath}Microsoft.Extensions.DependencyInjection.dll",
                $@"{aspBasePath}Microsoft.Extensions.Logging.dll",
                $@"{aspBasePath}Microsoft.Extensions.Configuration.dll",
            };

            ReadRuntimeAssemblyFiles(arr);
        }
        private static void ReadRuntimeAssemblyFiles(string[] allFiles)
        {
            try
            {
                // Read File content in 4K chunks
                int maxBuffer = 4 * 1024;
                byte[] chunk = new byte[maxBuffer];
                Random random = new Random();
                foreach (string file in allFiles)
                {
                    // Read file content to avoid disk reads during specialization. This is only to page-in bytes.
                    ReadFileInChunks(file, chunk, maxBuffer, random);
                }
                Logger.Log($"Preloader.ReadRuntimeAssemblyFiles Total files read:{allFiles.Length}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in Preloader.ReadRuntimeAssemblyFiles:{ex}");
            }
        }

        private static void ReadFileInChunks(string file, byte[] chunk, int maxBuffer, Random random)
        {
            var fileExist = File.Exists(file);
            Logger.Log($"Reading {file}. file exist:{fileExist}");

            if (!fileExist)
            {
                return;
            }

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
                Logger.Log($"Failed to read file {file}. {ex}");
            }
        }
    }
}
