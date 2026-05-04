// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Mono.Cecil;
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

    private readonly FunctionsAssemblyResolver _resolver;
    private readonly ReaderParameters _readerParameters;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionsAssemblyScanner"/> class.
    /// </summary>
    /// <param name="searchDirectories">The search directories to include, if any.</param>
    public FunctionsAssemblyScanner(IEnumerable<string>? searchDirectories = null)
    {
        _resolver = new();
        _readerParameters = new ReaderParameters
        {
            AssemblyResolver = _resolver,
        };

        if (searchDirectories != null)
        {
            foreach (string directory in searchDirectories)
            {
                _resolver.AddSearchDirectory(directory);
            }
        }
    }

    /// <summary>
    /// Creates a <see cref="FunctionsAssemblyScanner"/> from the given task items. Uses
    /// the directories of the task items as search directories.
    /// </summary>
    /// <param name="items">The items to use parent directories from.</param>
    /// <returns>A <see cref="FunctionsAssemblyScanner" /> with search directories added.</returns>
    public static FunctionsAssemblyScanner FromTaskItems(IEnumerable<ITaskItem> items)
    {
        IEnumerable<string> searchDirectories = items
            .Select(item => Path.GetDirectoryName(item.ItemSpec))
            .Where(dir => !string.IsNullOrEmpty(dir))
            .Distinct();

        return new(searchDirectories);
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
    /// Gets the WebJobs references, if any, from the given assembly path.
    /// </summary>
    /// <param name="assembly">The disk path of the assembly to scan.</param>
    /// <param name="logger">The optional logger.</param>
    /// <returns>The found WebJobs references, if any.</returns>
    public IEnumerable<WebJobsReference> GetWebJobsReferences(string assembly, ILogger? logger = null)
    {
        Throw.IfNullOrEmpty(assembly);
        AssemblyDefinition definition = AssemblyDefinition.ReadAssembly(assembly, _readerParameters);
        return WebJobsReference.FromModule(definition, logger);
    }
}
