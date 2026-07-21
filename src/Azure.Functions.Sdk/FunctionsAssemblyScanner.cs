// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using ILogger = NuGet.Common.ILogger;

namespace Azure.Functions.Sdk;

/// <summary>
/// Class to help with scanning functions related assemblies.
/// </summary>
public static class FunctionsAssemblyScanner
{
    private const string ExtensionsInformationType = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.ExtensionInformationAttribute";
    private static readonly Regex ExcludedPackagesRegex = new(
        @"^(System|Azure\.Core|Azure\.Identity|Microsoft\.Bcl|Microsoft\.Extensions|Microsoft\.Identity|Microsoft\.NETCore|Microsoft\.NETStandard|Microsoft\.Win32|Grpc|OpenTelemetry)(\..*|$)",
        RegexOptions.Compiled);

    /// <summary>
    /// Checks if the given package name should be scanned or not.
    /// </summary>
    /// <param name="name">The name of the package.</param>
    /// <returns><c>true</c> if package should be scanned, <c>false</c> otherwise.</returns>
    public static bool ShouldScanPackage(string name)
    {
        return !string.IsNullOrEmpty(name) && !ExcludedPackagesRegex.IsMatch(name);
    }

    /// <summary>
    /// Gets the WebJobs references, if any, from the given assembly path.
    /// </summary>
    /// <param name="assembly">The disk path of the assembly to scan.</param>
    /// <param name="logger">The optional logger.</param>
    /// <returns>The found WebJobs references, if any.</returns>
    public static IReadOnlyList<WebJobsReference> GetWebJobsReferences(string assembly, ILogger? logger = null)
    {
        Throw.IfNullOrEmpty(assembly);
        using FileStream stream = File.OpenRead(assembly);
        using PEReader peReader = new(stream);
        if (!peReader.HasMetadata)
        {
            return [];
        }

        // The MetadataReader reads from memory owned by the PEReader, so results must be materialized
        // (FromModule returns a concrete list) before the PEReader is disposed.
        return WebJobsReference.FromModule(peReader.GetMetadataReader(), assembly, logger);
    }

    /// <summary>
    /// Tries to get an extension reference from the given assembly path.
    /// </summary>
    /// <param name="assembly">The disk path of the assembly to scan.</param>
    /// <param name="sourcePackageId">The source package ID.</param>
    /// <param name="extensionReference">The resulting extension reference, if found.</param>
    /// <returns><c>true</c> if an extension reference was found; <c>false</c> otherwise.</returns>
    public static bool TryGetExtensionReference(
        string assembly, string sourcePackageId, [NotNullWhen(true)] out ITaskItem? extensionReference)
    {
        Throw.IfNullOrEmpty(assembly);
        using FileStream stream = File.OpenRead(assembly);
        using PEReader peReader = new(stream);
        if (!peReader.HasMetadata)
        {
            extensionReference = null;
            return false;
        }

        // ExtensionReference builds an ITaskItem holding only strings, so it is safe after the
        // PEReader is disposed.
        return TryGetExtensionFromAssembly(
            peReader.GetMetadataReader(), sourcePackageId, out extensionReference);
    }

    /// <summary>
    /// Gets the extension reference, if any, from the given assembly metadata.
    /// </summary>
    /// <param name="reader">The metadata reader for the assembly to scan.</param>
    /// <param name="sourcePackageId">The source package ID.</param>
    /// <param name="extensionReference">The resulting extension reference, if found.</param>
    /// <returns><c>true</c> if an extension reference was found; <c>false</c> otherwise.</returns>
    private static bool TryGetExtensionFromAssembly(
        MetadataReader reader,
        string sourcePackageId,
        [NotNullWhen(true)] out ITaskItem? extensionReference)
    {
        foreach (CustomAttributeHandle handle in reader.GetAssemblyDefinition().GetCustomAttributes())
        {
            CustomAttribute attribute = reader.GetCustomAttribute(handle);

            // Match by metadata type name only. Reading the name does not resolve the assembly that
            // defines the attribute, so we can detect the extension even when that assembly is absent.
            if (!string.Equals(
                    reader.GetAttributeTypeName(attribute),
                    ExtensionsInformationType,
                    StringComparison.Ordinal))
            {
                continue;
            }

            ImmutableArray<CustomAttributeTypedArgument<string>> arguments =
                attribute.DecodeArguments().FixedArguments;
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
