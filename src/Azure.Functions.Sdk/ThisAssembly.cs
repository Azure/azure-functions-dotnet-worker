// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.Functions.Sdk;

internal static partial class ThisAssembly
{
    /// <summary>
    /// Gets the name of the Azure Functions SDK module.
    /// </summary>
    public static string Name { get; } = typeof(ThisAssembly).Assembly.GetName().Name!;

    /// <summary>
    /// Gets the version of the Azure Functions SDK module.
    /// </summary>
    public static Version Version { get; } = typeof(ThisAssembly).Assembly.GetName().Version!;

    /// <summary>
    /// Gets the module version ID of the assembly. This acts as an assembly hash, changing any time the source code changes.
    /// </summary>
    public static string ModuleVersionId { get; } = typeof(ThisAssembly).Assembly.ManifestModule.ModuleVersionId.ToString();
}
