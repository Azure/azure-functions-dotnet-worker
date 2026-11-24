// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Abstractions;
using Microsoft.Build.Framework;
using NuGet.Packaging;
using NuGet.ProjectModel;
using NuGet.Versioning;

namespace Azure.Functions.Sdk.Tasks.Publish;

public sealed class ResolveFunctionsContainerRuntimePacks(IFileSystem fileSystem) : Microsoft.Build.Utilities.Task
{
    private readonly IFileSystem _fileSystem = Throw.IfNull(fileSystem);

    public ResolveFunctionsContainerRuntimePacks()
        : this(new FileSystem())
    {
    }

    [Required]
    public string ProjectAssetsFile { get; set; } = string.Empty;

    [Required]
    public string TargetFramework { get; set; } = string.Empty;

    [Required]
    public string RuntimeTargetFramework { get; set; } = string.Empty;

    [Required]
    public string CoreRuntimeVersion { get; set; } = string.Empty;

    [Required]
    public string AspNetRuntimeVersion { get; set; } = string.Empty;

    [Output]
    public string CorePackPath { get; private set; } = string.Empty;

    [Output]
    public string AspNetPackPath { get; private set; } = string.Empty;

    public override bool Execute()
    {
        if (!_fileSystem.File.Exists(ProjectAssetsFile))
        {
            Log.LogError(Strings.MissingProjectAssetsFile, ProjectAssetsFile);
            return false;
        }

        using FileSystemStream stream = _fileSystem.File.OpenRead(ProjectAssetsFile);
        LockFile lockFile = LockFile.Read(ProjectAssetsFile, stream, new MSBuildNugetLogger(Log));
        TargetFrameworkInformation? framework = lockFile.PackageSpec?.TargetFrameworks.FirstOrDefault(item =>
            string.Equals(item.TargetAlias, TargetFramework, StringComparison.OrdinalIgnoreCase)
            || item.FrameworkName.GetShortFolderName().Equals(TargetFramework, StringComparison.OrdinalIgnoreCase));
        if (framework is null)
        {
            Log.LogError(Strings.Container_RuntimeDownloadsMissing, TargetFramework, ProjectAssetsFile);
            return false;
        }

        FallbackPackagePathResolver pathResolver = lockFile.GetPathResolver();
        if (!TryResolvePack(framework, pathResolver, GetFunctionsContainerRuntimeDownloads.CorePackage, CoreRuntimeVersion, out string corePath)
            || !TryResolvePack(framework, pathResolver, GetFunctionsContainerRuntimeDownloads.AspNetPackage, AspNetRuntimeVersion, out string aspNetPath))
        {
            return false;
        }

        CorePackPath = _fileSystem.Path.Combine(corePath, "runtimes", "linux-x64");
        AspNetPackPath = _fileSystem.Path.Combine(aspNetPath, "runtimes", "linux-x64");
        if (!_fileSystem.File.Exists(_fileSystem.Path.Combine(CorePackPath, "native", "libhostfxr.so"))
            || !_fileSystem.File.Exists(_fileSystem.Path.Combine(CorePackPath, "native", "libcoreclr.so"))
            || !_fileSystem.Directory.Exists(_fileSystem.Path.Combine(CorePackPath, "lib", RuntimeTargetFramework))
            || !_fileSystem.File.Exists(_fileSystem.Path.Combine(AspNetPackPath, "lib", RuntimeTargetFramework, "Microsoft.AspNetCore.App.deps.json")))
        {
            Log.LogError(Strings.Container_RuntimePackIncomplete, CorePackPath, AspNetPackPath);
            return false;
        }

        return true;
    }

    private bool TryResolvePack(
        TargetFrameworkInformation framework,
        FallbackPackagePathResolver pathResolver,
        string packageId,
        string version,
        out string path)
    {
        path = string.Empty;
        if (!NuGetVersion.TryParse(version, out NuGetVersion? parsed)
            || !framework.DownloadDependencies.Any(item =>
                item.Name.Equals(packageId, StringComparison.OrdinalIgnoreCase) && item.VersionRange.Satisfies(parsed)))
        {
            Log.LogError(Strings.Container_RuntimeDownloadsMissing, TargetFramework, ProjectAssetsFile);
            return false;
        }

        string? packagePath = pathResolver.GetPackageDirectory(packageId, parsed);
        if (packagePath is null)
        {
            Log.LogError(Strings.Container_RuntimePackNotFound, packageId, version);
            return false;
        }

        path = packagePath;
        return true;
    }
}
