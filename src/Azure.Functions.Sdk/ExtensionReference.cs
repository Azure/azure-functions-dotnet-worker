// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace Azure.Functions.Sdk;

/// <summary>
/// Represents an Azure Functions extension reference.
/// </summary>
public static class ExtensionReference
{
    private const string ExtensionsInformationType = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.ExtensionInformationAttribute";

    /// <summary>
    /// Gets all extension references from an assembly module.
    /// </summary>
    /// <param name="path">The path to the assembly.</param>
    /// <param name="sourcePackageId">The source package ID.</param>
    /// <returns>A list of extension references found in the assembly.</returns>
    public static List<ITaskItem> GetFromModule(string path, string sourcePackageId)
    {
        var results = new List<ITaskItem>();

        // Read the assembly from the specified path
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path);

        // Find all extension attributes
        foreach (CustomAttribute customAttribute in assembly.CustomAttributes)
        {
            if (string.Equals(
                    customAttribute.AttributeType.FullName,
                    ExtensionsInformationType,
                    StringComparison.Ordinal))
            {
                customAttribute.GetArguments(out string name, out string version);
                results.Add(new TaskItem(name)
                {
                    Version = version,
                    IsImplicitlyDefined = true,
                    SourcePackageId = sourcePackageId,
                });
            }
        }

        return results;
    }
}
