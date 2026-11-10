// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Azure.Functions.Worker.Core.Configuration
{
    internal sealed class PhysicalHostConfigurationFileSystem : IHostConfigurationFileSystem
    {
        public Stream OpenRead(string path)
        {
            try
            {
                return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 4096, FileOptions.SequentialScan);
            }
            catch (FileNotFoundException exception)
            {
                throw new HostConfigurationFileException(HostConfigurationFailureReason.FileNotFound, exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                throw new HostConfigurationFileException(HostConfigurationFailureReason.AccessDenied, exception);
            }
            catch (Exception exception)
            {
                throw new HostConfigurationFileException(HostConfigurationFailureReason.OpenFailed, exception);
            }
        }
    }
}
