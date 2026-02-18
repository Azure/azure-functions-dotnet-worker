// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests;

internal static class ModuleInitializer
{
    private static readonly string ResolverPath = Path.Combine(
        Path.GetDirectoryName(GetAssemblyLocation())!, "resolver");

    /// <summary>
    /// We cannot include MSBuild assemblies in our output, because they will interfere with
    /// MSBuilds assembly scanning. Instead we use the MSBuildLocator to resolve them at runtime.
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        Environment.SetEnvironmentVariable("MSBUILDADDITIONALSDKRESOLVERSFOLDER", ResolverPath);
        MSBuildAssemblyResolver.Register();
        FormatterResolver.Initialize();
    }

    private static string GetAssemblyLocation()
    {
#if NET
        return typeof(ModuleInitializer).Assembly.Location;
#else
        Uri uri = new Uri(typeof(ModuleInitializer).Assembly.CodeBase!);
        return uri.AbsolutePath;
#endif
    }
}
