// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Mono.Cecil;

namespace Azure.Functions.Sdk;

/// <summary>
/// An assembly resolver that skips trusted platform assemblies.
/// </summary>
public class FunctionsAssemblyResolver : DefaultAssemblyResolver
{
    private static readonly ImmutableHashSet<string> TrustedPlatformAssemblies
        = GetTrustedPlatformAssemblies();

    private static ImmutableHashSet<string> GetTrustedPlatformAssemblies()
    {
        object? data = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (data is null)
        {
#pragma warning disable IDE0301 // Simplify collection initialization
            // Collection expression '[]' fails at runtime.
            return ImmutableHashSet<string>.Empty;
#pragma warning restore IDE0301 // Simplify collection initialization
        }

        return data.ToString().Split(Path.PathSeparator)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolves the assembly, returning null if it's a trusted platform assembly.
    /// </summary>
    /// <param name="name">The assembly name reference.</param>
    /// <returns>The assembly definition, if successfully resolved.</returns>
    public override AssemblyDefinition? Resolve(AssemblyNameReference name)
    {
        // As soon as we get to a TPA we can stop. This helps prevent the odd circular reference
        // with type forwarders as well.
        AssemblyDefinition assemblyDef = base.Resolve(name);
        return TrustedPlatformAssemblies.Contains(assemblyDef.MainModule.FileName) ? null : assemblyDef;
    }
}
