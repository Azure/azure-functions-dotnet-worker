// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Microsoft.Build.Framework;
using NuGet.LibraryModel;
using NuGet.Packaging;
using NuGet.ProjectModel;

namespace Azure.Functions.Sdk.Tasks.Extensions;

public class ResolveExtensionPackages(IFileSystem fileSystem)
    : Microsoft.Build.Utilities.Task, ICancelableTask, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly IFileSystem _fileSystem = Throw.IfNull(fileSystem);

    public ResolveExtensionPackages()
        : this(new FileSystem())
    {
    }

    [Required]
    public string RestoreOutputPath { get; set; } = string.Empty;

    [Output]
    public ITaskItem[] ExtensionPackages { get; private set; } = [];

    public void Cancel()
    {
        _cts.Cancel();
    }

    public void Dispose()
    {
        _cts.Dispose();
    }

    public override bool Execute()
    {
        if (!TryGetLockFile(out LockFile? lockFile))
        {
            return false;
        }

        List<ITaskItem> extensionPackages = [];
        FallbackPackagePathResolver resolver = lockFile.GetPathResolver();
        foreach (LockFileTarget target in lockFile.Targets)
        {
            if (!string.IsNullOrEmpty(target.RuntimeIdentifier))
            {
                // Scanning specific RID's will be redundant as packages can only condition on TFM.
                continue;
            }

            if (_cts.IsCancellationRequested)
            {
                return false;
            }

            foreach (ITaskItem ext in GetExtensionPackages(target, resolver))
            {
                extensionPackages.Add(ext);
            }
        }

        if (_cts.IsCancellationRequested)
        {
            return false;
        }

        ExtensionPackages = [.. extensionPackages];
        return true;
    }

    private static bool ShouldScanLibrary(LockFileTargetLibrary library)
    {
        return library.Type == LibraryType.Package
            && library.Name is not null
            && !FunctionsAssemblyScanner.IsExcludedPackage(library.Name);
    }

    private bool TryGetLockFile([NotNullWhen(true)] out LockFile? lockFile)
    {
        if (_cts.IsCancellationRequested)
        {
            lockFile = null;
            return false;
        }

        string assetsFile = Path.Combine(RestoreOutputPath, LockFileFormat.AssetsFileName);
        if (!_fileSystem.File.Exists(assetsFile))
        {
            Log.LogError("Assets file '{0}' does not exist. Please ensure restore successfully ran.", assetsFile);
            lockFile = null;
            return false;
        }

        IFileInfo info = _fileSystem.FileInfo.New(assetsFile);
        using FileSystemStream stream = info.OpenRead();
        LockFileFormat format = new();
        lockFile = format.Read(stream, new MSBuildNugetLogger(Log), assetsFile);
        return true;
    }

    private IEnumerable<ITaskItem> GetExtensionPackages(LockFileTarget target, FallbackPackagePathResolver resolver)
    {
        foreach (LockFileTargetLibrary library in target.Libraries)
        {
            if (_cts.IsCancellationRequested)
            {
                yield break;
            }

            if (!ShouldScanLibrary(library))
            {
                continue;
            }

            string packagePath = resolver.GetPackageDirectory(library.Name, library.Version);
            foreach (LockFileItem assembly in library.RuntimeAssemblies)
            {
                string path = Path.Combine(packagePath, assembly.Path);
                if (ExtensionReference.TryGetFromModule(path, library.Name!, out ITaskItem? ext))
                {
                    Log.LogMessage(
                        MessageImportance.Low,
                        "Found extension: {0}/{1}. Reference by: {2}/{3}",
                        ext.ItemSpec,
                        ext.GetMetadata("Version"),
                        library.Name,
                        library.Version);

                    yield return ext;
                }
            }
        }
    }
}
