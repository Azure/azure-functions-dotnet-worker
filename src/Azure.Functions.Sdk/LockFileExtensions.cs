// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using NuGet.Common;
using NuGet.Packaging;
using NuGet.ProjectModel;

namespace Azure.Functions.Sdk;

/// <summary>
/// Extensions for NuGet lock files.
/// </summary>
public static class LockFileExtensions
{
    extension(LockFile)
    {
        /// <summary>
        /// Reads a lock file from the given path.
        /// </summary>
        /// <param name="path">The path to read.</param>
        /// <param name="logger">The optional logger.</param>
        /// <returns>The read <see cref="LockFile"/>.</returns>
        public static LockFile Read(string path, ILogger? logger = null)
        {
            Throw.IfNullOrEmpty(path);
            LockFileFormat format = new();
            return logger is null ? format.Read(path) : format.Read(path, logger);
        }

        /// <summary>
        /// Reads a lock file from the given stream.
        /// </summary>
        /// <param name="path">The path for <see cref="LockFile.Path"/>.</param>
        /// <param name="stream">The stream to read.</param>
        /// <param name="logger">The optional logger.</param>
        /// <returns>The read <see cref="LockFile"/>.</returns>
        public static LockFile Read(string path, Stream stream, ILogger? logger = null)
        {
            Throw.IfNullOrEmpty(path);
            Throw.IfNull(stream);

            LockFileFormat format = new();
            return logger is null ? format.Read(stream, path) : format.Read(stream, logger, path);
        }
    }

    extension(LockFile lockFile)
    {
        /// <summary>
        /// Gets a package path resolver for the given lock file.
        /// </summary>
        /// <returns>A package path resolver.</returns>
        public FallbackPackagePathResolver GetPathResolver()
        {
            Throw.IfNull(lockFile);
            string? userPackageFolder = lockFile.PackageFolders.FirstOrDefault()?.Path;
            return new(userPackageFolder, lockFile.PackageFolders.Skip(1).Select(folder => folder.Path));
        }
    }
}
