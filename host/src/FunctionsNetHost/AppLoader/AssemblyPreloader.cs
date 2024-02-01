// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using FunctionsNetHost.Diagnostics;

namespace FunctionsNetHost
{
    internal static class AssemblyPreloader
    {
        internal static void Preload()
        {
            var assemblies = GetAssembliesToPreload();
            Logger.Log($"Preloading {assemblies.Length} assemblies");

            foreach (var assembly in assemblies)
            {
                var loaded = NativeLibrary.TryLoad(assembly, out _);
                Logger.LogTrace($"Preloaded {assembly} : {loaded}");
            }

            AppLoaderEventSource.Log.AssembliesPreloaded(assemblies.Length);
        }

        private static string[] GetAssembliesToPreload()
        {
            var assemblies = new string[]
                {
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Private.CoreLib.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App\\8.0.1\\Microsoft.Extensions.Diagnostics.Abstractions.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Text.Json.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\coreclr.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Linq.Expressions.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Collections.Immutable.dll",
                  "C:\\Program Files\\dotnet\\host\\fxr\\8.0.1\\hostfxr.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\hostpolicy.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Net.Http.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\clrjit.dll",
                  "C:\\Program Files (x86)\\SiteExtensions\\Functions\\4.99.5\\workers\\dotnet-isolated\\bin\\nethost.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Linq.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Text.RegularExpressions.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Collections.dll",
                  "C:\\Windows\\System32\\icu.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Text.Encodings.Web.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Net.Primitives.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App\\8.0.1\\Microsoft.Extensions.DependencyInjection.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Collections.Concurrent.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App\\8.0.1\\Microsoft.Extensions.Hosting.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Private.Uri.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Runtime.InteropServices.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Reflection.Emit.Lightweight.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App\\8.0.1\\Microsoft.Extensions.DependencyInjection.Abstractions.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Threading.ThreadPool.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Text.Encoding.Extensions.dll",
                  "C:\\Windows\\System32\\picohelper.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Threading.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Diagnostics.DiagnosticSource.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Runtime.Loader.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App\\8.0.1\\Microsoft.Extensions.Hosting.Abstractions.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App\\8.0.1\\Microsoft.Extensions.FileProviders.Physical.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App\\8.0.1\\Microsoft.Extensions.Options.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Numerics.Vectors.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App\\8.0.1\\Microsoft.Extensions.Configuration.EnvironmentVariables.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Diagnostics.Tracing.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Security.Claims.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App\\8.0.1\\Microsoft.Extensions.Logging.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.ComponentModel.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App\\8.0.1\\Microsoft.Extensions.Diagnostics.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Threading.Channels.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App\\8.0.1\\Microsoft.Extensions.Configuration.FileExtensions.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\netstandard.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Runtime.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Reflection.Emit.ILGeneration.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Reflection.Primitives.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App\\8.0.1\\Microsoft.Extensions.FileProviders.Abstractions.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Runtime.InteropServices.RuntimeInformation.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App\\8.0.1\\Microsoft.Extensions.Configuration.CommandLine.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App\\8.0.1\\Microsoft.Extensions.Configuration.Abstractions.dll",
                  "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\8.0.1\\System.Runtime.Intrinsics.dll"
                };

            return assemblies;
        }
    }
}
