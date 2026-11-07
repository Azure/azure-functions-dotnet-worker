// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Azure.Functions.Worker.Grpc;

/// <summary>
/// Resolves explicitly configured worker-local application roots without falling back to Host, installation, or current-directory paths.
/// </summary>
internal static class WorkerApplicationDirectory
{
    internal const string EnvironmentVariable = "FUNCTIONS_APPLICATION_DIRECTORY";

    /// <summary>
    /// Reads the configured root and returns an existing, fully qualified directory with a trailing separator.
    /// Returns null when unset; malformed paths and access failures propagate.
    /// </summary>
    internal static string? GetConfigured()
    {
        string? path = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))
        {
            throw new InvalidOperationException($"{EnvironmentVariable} must specify a fully qualified worker-local application directory.");
        }

        string pathRoot = Path.GetPathRoot(path)!;
        if (Path.DirectorySeparatorChar == '\\' && (pathRoot.Length < 2 || pathRoot[pathRoot.Length - 1] == ':'))
        {
            throw new InvalidOperationException($"{EnvironmentVariable} must specify a fully qualified worker-local application directory.");
        }

        string fullPath = Path.GetFullPath(path);
        if (fullPath.Length > Path.GetPathRoot(fullPath)!.Length)
        {
            fullPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        if ((File.GetAttributes(fullPath) & FileAttributes.Directory) == 0)
        {
            throw new InvalidDataException("The worker-local application root must be a directory.");
        }

        return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }

    /// <summary>
    /// Uses the same root validation but throws when no application directory is configured.
    /// </summary>
    internal static string GetRequired() => GetConfigured()
        ?? throw new InvalidOperationException($"{EnvironmentVariable} must specify a fully qualified worker-local application directory.");
}
