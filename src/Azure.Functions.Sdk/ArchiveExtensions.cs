// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using Azure.Functions.Sdk;

namespace System.IO.Compression;

internal static class ArchiveExtensions
{
    /// <summary>
    /// Sets the unix file permissions for the zip entry.
    /// </summary>
    /// <param name="entry">The zip archive entry.</param>
    /// <param name="unixPermissions">The file permissions to set.</param>
    public static void SetUnixFilePermissions(this ZipArchiveEntry entry, int unixPermissions)
    {
        Throw.IfNull(entry);

        // Set the external attributes to include the unix permissions.
        // The upper two bytes are the unix permissions and file type.
        entry.SetExternalAttributes(unixPermissions << 16 | entry.GetExternalAttributes() & 0xFFFF);
    }

    private static void SetExternalAttributes(this ZipArchiveEntry entry, int attributes)
    {
        Throw.IfNull(entry);
        typeof(ZipArchiveEntry)
            .GetProperty("ExternalAttributes", BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(entry, attributes);
    }

    private static int GetExternalAttributes(this ZipArchiveEntry entry)
    {
        Throw.IfNull(entry);
        return (int)typeof(ZipArchiveEntry)
            .GetProperty("ExternalAttributes", BindingFlags.Public | BindingFlags.Instance)!
            .GetValue(entry)!;
    }
}
