// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Abstractions;

namespace Azure.Functions.Sdk;

/// <summary>
/// Extension methods for <see cref="IFileSystem"/>.
/// </summary>
internal static class FileSystemExtensions
{
    extension(IFile file)
    {
        /// <summary>
        /// Moves a file to a new location, optionally overwriting an existing file.
        /// </summary>
        /// <param name="sourceFileName">The source file to move.</param>
        /// <param name="destFileName">The destination file path.</param>
        /// <param name="overwrite">Whether to overwrite the destination file if it exists.</param>
        public void Move(string sourceFileName, string destFileName, bool overwrite)
        {
            Throw.IfNull(file);
            Throw.IfNullOrWhitespace(sourceFileName);
            Throw.IfNullOrWhitespace(destFileName);

            if (overwrite && file.Exists(destFileName))
            {
                file.Replace(sourceFileName, destFileName, null);
                return;
            }

            file.Move(sourceFileName, destFileName);
        }

        /// <summary>
        /// Attempts to delete a file, swallowing any IO exceptions that may occur.
        /// </summary>
        /// <param name="path">The path of the file to delete.</param>
        public void TryDelete(string path)
        {
            Throw.IfNull(file);
            Throw.IfNullOrWhitespace(path);

            try
            {
                file.Delete(path);
            }
            catch (IOException)
            {
                // Ignore IO exceptions when deleting a file.
            }
        }
    }
}
