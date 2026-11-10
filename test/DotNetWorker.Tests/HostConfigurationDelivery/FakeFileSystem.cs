// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.Azure.Functions.Worker.Core.Configuration;

namespace Microsoft.Azure.Functions.Worker.Tests.HostConfigurationDelivery
{
    internal sealed class FakeFileSystem : IHostConfigurationFileSystem
    {
        private readonly Func<string, Stream> _open;
        private int _opens;

        public FakeFileSystem(Func<Stream> open) => _open = _ => open();

        public FakeFileSystem(Func<string, Stream> open) => _open = open;

        public int Opens => _opens;

        public Stream OpenRead(string path)
        {
            Interlocked.Increment(ref _opens);
            return _open(path);
        }
    }
}
