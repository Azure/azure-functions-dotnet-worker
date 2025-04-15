// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using IOPath = System.IO.Path;

namespace Microsoft.Azure.Functions.Sdk.E2ETests
{
    public sealed class TempDirectory : IDisposable
    {
        public TempDirectory() : this(IOPath.Combine(IOPath.GetTempPath(), IOPath.GetRandomFileName()))
        {
        }

        public TempDirectory(string path)
        {
            ArgumentNullException.ThrowIfNull(path);
            Path = path;

            if (!Directory.Exists(Path))
            {
                Directory.CreateDirectory(Path);
            }
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, true);
            }
            catch (IOException)
            {
                // Ignore IO exceptions during cleanup
            }
        }
    }
}
