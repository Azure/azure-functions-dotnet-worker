// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using NuGet.Packaging;
using NuGet.ProjectModel;

namespace Azure.Functions.Sdk;

public static class LockFileExtensions
{
    public static FallbackPackagePathResolver GetPathResolver(this LockFile lockFile)
    {
        Throw.IfNull(lockFile);
        string? userPackageFolder = lockFile.PackageFolders.FirstOrDefault()?.Path;
        return new(userPackageFolder, lockFile.PackageFolders.Skip(1).Select(folder => folder.Path));
    }
}
