// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.Azure.Functions.Worker.Core.Configuration
{
    internal interface IHostConfigurationFileSystem
    {
        /// <exception cref="HostConfigurationFileException">
        /// The file could not be opened. Read and disposal failures are not wrapped.
        /// </exception>
        Stream OpenRead(string path);
    }
}
