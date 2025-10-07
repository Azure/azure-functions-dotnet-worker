// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Mono.Cecil;

namespace Azure.Functions.Sdk;

public class FunctionsAssemblyResolver : DefaultAssemblyResolver
{
    private static readonly ImmutableHashSet<string> TrustedPlatformAssemblies
        = GetTrustedPlatformAssemblies();

    private static ImmutableHashSet<string> GetTrustedPlatformAssemblies()
    {
        var data = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        return data.ToString().Split(Path.PathSeparator)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public override AssemblyDefinition? Resolve(AssemblyNameReference name)
    {
        // As soon as we get to a TPA we can stop. This helps prevent the odd circular reference
        // with type forwarders as well.
        AssemblyDefinition assemblyDef = base.Resolve(name);
        return TrustedPlatformAssemblies.Contains(assemblyDef.MainModule.FileName) ? null : assemblyDef;
    }
}
