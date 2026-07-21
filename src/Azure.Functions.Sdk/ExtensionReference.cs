// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
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
    /// Gets the extension reference, if any, from the given assembly metadata.
    /// </summary>
    /// <param name="reader">The metadata reader for the assembly to scan.</param>
    /// <param name="sourcePackageId">The source package ID.</param>
    /// <param name="extensionReference">The resulting extension reference, if found.</param>
    /// <returns><c>true</c> if an extension reference was found; <c>false</c> otherwise.</returns>
    public static bool TryGetFromAssembly(
        MetadataReader reader,
        string sourcePackageId,
        [NotNullWhen(true)] out ITaskItem? extensionReference)
    {
        Throw.IfNull(reader);

        foreach (CustomAttributeHandle handle in reader.GetAssemblyDefinition().GetCustomAttributes())
        {
            CustomAttribute attribute = reader.GetCustomAttribute(handle);

            // Match by metadata type name only. Reading the name does not resolve the assembly that
            // defines the attribute, so we can detect the extension even when that assembly is absent.
            if (!string.Equals(
                    MetadataAttributeReader.GetAttributeTypeName(reader, attribute),
                    ExtensionsInformationType,
                    StringComparison.Ordinal))
            {
                continue;
            }

            ImmutableArray<CustomAttributeTypedArgument<string>> arguments =
                MetadataAttributeReader.DecodeArguments(attribute).FixedArguments;
            if (arguments.Length < 2)
            {
                continue;
            }

            string name = arguments[0].Value as string ?? string.Empty;
            string version = arguments[1].Value as string ?? string.Empty;
            extensionReference = new TaskItem(name)
            {
                Version = version,
                IsImplicitlyDefined = true,
                SourcePackageId = sourcePackageId,
            };

            return true;
        }

        extensionReference = default;
        return false;
    }
}
