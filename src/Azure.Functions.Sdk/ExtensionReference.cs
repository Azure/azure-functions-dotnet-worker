// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
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
    /// Gets the root path of the Azure Functions SDK module.
    /// </summary>
    /// <param name="path">The path to the assembly.</param>
    /// <param name="sourcePackageId">The source package ID.</param>
    /// <param name="extensionReference">The resulting extension reference, if found.</param>
    public static bool TryGetFromModule(
        string path,
        string sourcePackageId,
        [NotNullWhen(true)] out ITaskItem? extensionReference)
    {
        // Read the assembly from the specified path
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path);

        // Find the extension attribute
        foreach (CustomAttribute customAttribute in assembly.CustomAttributes)
        {
            if (string.Equals(
                    customAttribute.AttributeType.FullName,
                    ExtensionsInformationType,
                    StringComparison.Ordinal))
            {
                customAttribute.GetArguments(out string name, out string version);
                extensionReference = new TaskItem(name);
                extensionReference.SetVersion(version);
                extensionReference.SetIsImplicitlyDefined(true);
                extensionReference.SetSourcePackageId(sourcePackageId);

                return true;
            }
        }

        extensionReference = default;
        return false;
    }
}
