// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using ILogger = NuGet.Common.ILogger;

namespace Azure.Functions.Sdk;

/// <summary>
/// Class to help with scanning functions related assemblies.
/// </summary>
public sealed partial class FunctionsAssemblyScanner : IDisposable
{
    private static readonly Regex ExcludedPackagesRegex = new(
        @"^(System|Azure\.Core|Azure\.Identity|Microsoft\.Bcl|Microsoft\.Extensions|Microsoft\.Identity|Microsoft\.NETCore|Microsoft\.NETStandard|Microsoft\.Win32|Grpc|OpenTelemetry)(\..*|$)",
        RegexOptions.Compiled);

    private readonly MetadataLoadContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionsAssemblyScanner"/> class.
    /// </summary>
    /// <param name="assemblyPaths">The assembly file paths to use for dependency resolution, if any.</param>
    public FunctionsAssemblyScanner(IEnumerable<string>? assemblyPaths = null)
    {
        PathAssemblyResolver resolver = new(EnumerateAssemblyPaths(assemblyPaths));
        _context = new MetadataLoadContext(resolver);
    }

    /// <summary>
    /// Creates a <see cref="FunctionsAssemblyScanner"/> from the given task items, using their
    /// paths for dependency resolution.
    /// </summary>
    /// <param name="items">The items to use assembly paths from.</param>
    /// <returns>A <see cref="FunctionsAssemblyScanner" /> for the given items.</returns>
    public static FunctionsAssemblyScanner FromTaskItems(IEnumerable<ITaskItem> items)
    {
        Throw.IfNull(items);
        return new(items.Select(item => item.ItemSpec));
    }

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
    /// Enumerates the assembly file paths used to resolve dependencies while scanning. This includes the
    /// provided assembly paths plus the assemblies in the host runtime directory, which supply the core
    /// assembly and framework assemblies required by <see cref="MetadataLoadContext"/> to resolve
    /// <see cref="object"/> and walk base types up to <see cref="Attribute"/>.
    /// </summary>
    private static IEnumerable<string> EnumerateAssemblyPaths(IEnumerable<string>? assemblyPaths)
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        foreach (string path in assemblyPaths ?? [])
        {
            // The lock file will sometimes insert '_._' placeholders for assemblies not present on disk.
            if (IsAssemblyFile(path) && seen.Add(path) && File.Exists(path))
            {
                yield return path;
            }
        }

        // Framework assemblies are not part of the supplied paths, so pull them from the runtime directory.
        string runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();
        if (!Directory.Exists(runtimeDirectory))
        {
            yield break;
        }

        foreach (string file in Directory.EnumerateFiles(runtimeDirectory, "*.dll"))
        {
            if (seen.Add(file))
            {
                yield return file;
            }
        }
    }

    private static bool IsAssemblyFile(string path)
    {
        return !string.IsNullOrEmpty(path)
            && Path.GetExtension(path).ToLowerInvariant() is ".dll" or ".exe";
    }

    /// <summary>
    /// Gets the WebJobs references, if any, from the given assembly path.
    /// </summary>
    /// <param name="assembly">The disk path of the assembly to scan.</param>
    /// <param name="logger">The optional logger.</param>
    /// <returns>The found WebJobs references, if any.</returns>
    public IEnumerable<WebJobsReference> GetWebJobsReferences(string assembly, ILogger? logger = null)
    {
        Throw.IfNullOrEmpty(assembly);
        Assembly definition = _context.LoadFromAssemblyPath(assembly);
        return WebJobsReference.FromModule(definition, logger);
    }

    /// <summary>
    /// Tries to get an extension reference from the given assembly path.
    /// </summary>
    /// <param name="assembly">The disk path of the assembly to scan.</param>
    /// <param name="sourcePackageId">The source package ID.</param>
    /// <param name="extensionReference">The resulting extension reference, if found.</param>
    /// <returns><c>true</c> if an extension reference was found; <c>false</c> otherwise.</returns>
    public bool TryGetExtensionReference(
        string assembly, string sourcePackageId, [NotNullWhen(true)] out ITaskItem? extensionReference)
    {
        Throw.IfNullOrEmpty(assembly);
        Assembly definition = _context.LoadFromAssemblyPath(assembly);
        return ExtensionReference.TryGetFromAssembly(definition, sourcePackageId, out extensionReference);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _context.Dispose();
    }
}
