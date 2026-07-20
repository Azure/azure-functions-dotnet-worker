// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Azure.Functions.Sdk;

/// <summary>
/// Represents an Azure Functions extension reference.
/// </summary>
public static class ExtensionReference
{
    private const string ExtensionsInformationType = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.ExtensionInformationAttribute";

    /// <summary>
    /// Gets the extension reference, if any, from the given assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="sourcePackageId">The source package ID.</param>
    /// <param name="extensionReference">The resulting extension reference, if found.</param>
    /// <returns><c>true</c> if an extension reference was found; <c>false</c> otherwise.</returns>
    public static bool TryGetFromAssembly(
        Assembly assembly,
        string sourcePackageId,
        [NotNullWhen(true)] out ITaskItem? extensionReference)
    {
        Throw.IfNull(assembly);

        // Find the extension attribute
        foreach (CustomAttributeData customAttribute in assembly.GetCustomAttributesData())
        {
            string? attributeTypeName;
            try
            {
                // Accessing AttributeType forces the metadata load context to resolve the assembly
                // defining the attribute. Tolerate unresolvable attribute types rather than failing.
                attributeTypeName = customAttribute.AttributeType.FullName;
            }
            catch (Exception ex) when (ex is FileNotFoundException or BadImageFormatException)
            {
                continue;
            }

            if (string.Equals(
                    attributeTypeName,
                    ExtensionsInformationType,
                    StringComparison.Ordinal))
            {
                customAttribute.GetArguments(out string name, out string version);
                extensionReference = new TaskItem(name)
                {
                    Version = version,
                    IsImplicitlyDefined = true,
                    SourcePackageId = sourcePackageId,
                };

                return true;
            }
        }

        extensionReference = default;
        return false;
    }
}
