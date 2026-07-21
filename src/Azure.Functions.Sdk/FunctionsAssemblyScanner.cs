// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using ILogger = NuGet.Common.ILogger;

namespace Azure.Functions.Sdk;

/// <summary>
/// Class to help with scanning functions related assemblies.
/// </summary>
public sealed partial class FunctionsAssemblyScanner
{
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
        return ExtensionReference.TryGetFromAssembly(
            peReader.GetMetadataReader(), sourcePackageId, out extensionReference);
    }
}
