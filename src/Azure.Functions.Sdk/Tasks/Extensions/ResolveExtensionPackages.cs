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
    public string ProjectAssetsFile { get; set; } = string.Empty;

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
        return !Log.HasLoggedErrors;
    }

    private static bool ShouldScanLibrary(LockFileTargetLibrary library)
    {
        return library.Type == LibraryType.Package
            && library.Name is not null
            && FunctionsAssemblyScanner.ShouldScanPackage(library.Name);
    }

    private bool TryGetLockFile([NotNullWhen(true)] out LockFile? lockFile)
    {
        if (_cts.IsCancellationRequested)
        {
            lockFile = null;
            return false;
        }

        if (!_fileSystem.File.Exists(ProjectAssetsFile))
        {
            Log.LogError(Strings.MissingProjectAssetsFile, ProjectAssetsFile);
            lockFile = null;
            return false;
        }

        using FileSystemStream stream = _fileSystem.File.OpenRead(ProjectAssetsFile);
        lockFile = LockFile.Read(ProjectAssetsFile, stream, new MSBuildNugetLogger(Log));
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
                string path = _fileSystem.Path.Combine(packagePath, assembly.Path);
                if (TryGetExtensionReference(path, library, out ITaskItem? ext))
                {
                    yield return ext;
                }
            }
        }
    }

    private bool TryGetExtensionReference(
        string path, LockFileTargetLibrary library, [NotNullWhen(true)] out ITaskItem? ext)
    {
        // Lock file will sometimes insert '_._' for assemblies that are not present on disk for a given RID.
        if (Path.GetExtension(path).ToLowerInvariant() is not (".dll" or ".exe") || !_fileSystem.File.Exists(path))
        {
            ext = null;
            return false;
        }

        try
        {
            if (ExtensionReference.TryGetFromModule(path, library.Name!, out ext))
            {
                Log.LogMessage(
                    MessageImportance.Low,
                    "Extension {0}/{1} referenced by {2}/{3}",
                    ext.ItemSpec,
                    ext.GetVersion(),
                    library.Name,
                    library.Version);

                return true;
            }
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            Log.LogError(
                "Failed to read assembly '{0}' from package '{1}/{2}'.\n{3}: {4}",
                path,
                library.Name,
                library.Version,
                ex.GetType(),
                ex.Message);
        }

        ext = null;
        return false;
    }
}
