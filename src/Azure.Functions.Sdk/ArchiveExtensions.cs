// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;

namespace System.IO.Compression;

internal static class ArchiveExtensions
{
    // netstandard2.0 does not have a setter for ExternalAttributes, so use reflection.
    // All runtimes for MSBuild that we support will have this attribute.
    private static readonly PropertyInfo _externalAttributes = typeof(ZipArchiveEntry)
        .GetProperty("ExternalAttributes", BindingFlags.Public | BindingFlags.Instance)!;

    extension(ZipArchiveEntry entry)
    {
        /// <summary>
        /// Sets the unix file permissions for the zip entry.
        /// </summary>
        /// <param name="entry">The zip archive entry.</param>
        /// <param name="unixPermissions">The file permissions to set.</param>
        public void SetUnixFilePermissions(int unixPermissions)
        {
            Throw.IfNull(entry);

            // Set the external attributes to include the unix permissions.
            // The upper two bytes are the unix permissions and file type.
            entry.SetExternalAttributes(unixPermissions << 16 | entry.GetExternalAttributes() & 0xFFFF);
        }

        private void SetExternalAttributes(int attributes)
        {
            Throw.IfNull(entry);
            _externalAttributes.SetValue(entry, attributes);
        }

        private int GetExternalAttributes()
        {
            Throw.IfNull(entry);
            return (int)_externalAttributes.GetValue(entry)!;
        }
    }
}
