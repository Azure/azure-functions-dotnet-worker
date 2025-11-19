// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using NuGet.Packaging;
using NuGet.ProjectModel;

namespace Azure.Functions.Sdk;

/// <summary>
/// Extensions for NuGet lock files.
/// </summary>
public static class LockFileExtensions
{
    /// <summary>
    /// Gets a package path resolver for the given lock file.
    /// </summary>
    /// <param name="lockFile">The lock file.</param>
    /// <returns></returns>
    public static FallbackPackagePathResolver GetPathResolver(this LockFile lockFile)
    {
        Throw.IfNull(lockFile);
        string? userPackageFolder = lockFile.PackageFolders.FirstOrDefault()?.Path;
        return new(userPackageFolder, lockFile.PackageFolders.Skip(1).Select(folder => folder.Path));
    }
}
