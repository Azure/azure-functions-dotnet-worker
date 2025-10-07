// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Mono.Cecil;
using ILogger = NuGet.Common.ILogger;

namespace Azure.Functions.Sdk;

public sealed partial class FunctionsAssemblyScanner
{
    private static readonly Regex ExcludedPackagesRegex = new(
        @"^(System|Azure\.Core|Azure\.Identity|Microsoft\.Bcl|Microsoft\.Extensions|Microsoft\.Identity|Microsoft\.NETCore|Microsoft\.NETStandard|Microsoft\.Win32|Grpc)\..*",
        RegexOptions.Compiled);

    private readonly IFileSystem _fileSystem;
    private readonly ReaderParameters _readerParameters;
    private readonly FunctionsAssemblyResolver _resolver;

    public FunctionsAssemblyScanner(
        IFileSystem? fileSystem = null,
        IEnumerable<string>? searchDirectories = null)
    {
        _resolver = new();
        _readerParameters = new ReaderParameters
        {
            AssemblyResolver = _resolver,
        };

        _fileSystem = fileSystem ?? new FileSystem();
        if (searchDirectories != null)
        {
            foreach (string directory in searchDirectories)
            {
                _resolver.AddSearchDirectory(directory);
            }
        }
    }

    public static FunctionsAssemblyScanner FromTaskItems(
        IEnumerable<ITaskItem> items, IFileSystem? fileSystem = null)
    {
        IEnumerable<string> searchDirectories = items
            .Select(item => Path.GetDirectoryName(item.ItemSpec))
            .Where(dir => !string.IsNullOrEmpty(dir))
            .Distinct();

        return new(fileSystem, searchDirectories);
    }

    public static bool IsExcludedPackage(string name)
    {
        return string.IsNullOrEmpty(name) || ExcludedPackagesRegex.IsMatch(name);
    }

    public IEnumerable<WebJobsReference> GetWebJobsReferences(string assembly, ILogger? logger = null)
    {
        AssemblyDefinition definition = ReadAssembly(assembly);
        return WebJobsReference.FromModule(definition, logger);
    }

    private AssemblyDefinition ReadAssembly(string assemblyPath)
    {
        Throw.IfNullOrEmpty(assemblyPath);
        return AssemblyDefinition.ReadAssembly(assemblyPath, _readerParameters);
    }
}
