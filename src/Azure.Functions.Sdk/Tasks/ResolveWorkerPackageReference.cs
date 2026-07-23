// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Microsoft.Build.Framework;
using NuGet.LibraryModel;
using NuGet.Frameworks;
using NuGet.ProjectModel;

namespace Azure.Functions.Sdk.Tasks;

public class ResolveWorkerPackageReference(IFileSystem fileSystem)
    : Microsoft.Build.Utilities.Task, ICancelableTask, IDisposable
{
    private const string WorkerPackageId = "Microsoft.Azure.Functions.Worker";

    private readonly CancellationTokenSource _cts = new();
    private readonly IFileSystem _fileSystem = Throw.IfNull(fileSystem);

    public ResolveWorkerPackageReference()
        : this(new FileSystem())
    {
    }

    [Required]
    public string ProjectAssetsFile { get; set; } = string.Empty;

    public string? TargetFramework { get; set; }

    [Output]
    public ITaskItem[] WorkerPackages { get; private set; } = [];

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
            return !_cts.IsCancellationRequested;
        }

        List<ITaskItem> workerPackages = [];
        NuGetFramework? currentFramework = TryParseFramework(TargetFramework);
        foreach (LockFileTarget target in lockFile!.Targets)
        {
            if (_cts.IsCancellationRequested)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(target.RuntimeIdentifier))
            {
                // Skip RID-specific targets to avoid duplicates.
                continue;
            }

            if (!IsCurrentTargetFramework(target, currentFramework))
            {
                continue;
            }

            LockFileTargetLibrary? library = target.Libraries.FirstOrDefault(IsWorkerPackage);
            if (library is null)
            {
                continue;
            }

            Microsoft.Build.Utilities.TaskItem item = new(library.Name!)
            {
                Version = library.Version?.ToNormalizedString() ?? string.Empty,
                TargetFramework = target.TargetFramework.ToString(),
            };

            workerPackages.Add(item);

            // If a specific TFM was requested, there is at most one relevant package entry.
            if (currentFramework is not null)
            {
                break;
            }
        }

        WorkerPackages = [.. workerPackages];

        if (WorkerPackages.Length == 0)
        {
            Log.LogMessage(LogMessage.Warning_WorkerPackageNotReferenced);
        }

        return !Log.HasLoggedErrors;
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
            // Don't fail when assets are missing. This can happen in DTB before restore.
            Log.LogMessage(
                Microsoft.Build.Framework.MessageImportance.Low,
                "Project assets file '{0}' was not found.",
                ProjectAssetsFile);

            lockFile = null;
            WorkerPackages = [];
            Log.LogMessage(LogMessage.Warning_WorkerPackageNotReferenced);
            return false;
        }

        using FileSystemStream stream = _fileSystem.File.OpenRead(ProjectAssetsFile);
        lockFile = LockFile.Read(ProjectAssetsFile, stream, new MSBuildNugetLogger(Log));
        return true;
    }

    private static bool IsWorkerPackage(LockFileTargetLibrary library)
    {
        return library.Type == LibraryType.Package
            && library.Name is not null
            && string.Equals(library.Name, WorkerPackageId, StringComparison.OrdinalIgnoreCase);
    }

    private static NuGetFramework? TryParseFramework(string? value)
    {
        string? framework = value?.Trim();
        if (string.IsNullOrWhiteSpace(framework))
        {
            return null;
        }

        try
        {
            return NuGetFramework.Parse(framework!);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsCurrentTargetFramework(LockFileTarget target, NuGetFramework? currentFramework)
    {
        if (currentFramework is null)
        {
            return true;
        }

        return target.TargetFramework is not null
            && target.TargetFramework.Equals(currentFramework);
    }
}
