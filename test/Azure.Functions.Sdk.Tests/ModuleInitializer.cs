﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.



using System.Runtime.CompilerServices;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests;

internal static class ModuleInitializer
{
    /// <summary>
    /// We cannot include MSBuild assemblies in our output, because they will interfere with
    /// MSBuilds assembly scanning. Instead we use the MSBuildLocator to resolve them at runtime.
    /// </summary>
    [ModuleInitializer]
    internal static void InitializeMSBuild()
    {
        MSBuildAssemblyResolver.Register();
    }
}
