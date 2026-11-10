// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Azure.Functions.Worker.Tests.HostConfigurationDelivery
{
    internal sealed class ChunkedStream : MemoryStream
    {
        private readonly int _chunkSize;

        public ChunkedStream(byte[] bytes, int chunkSize) : base(bytes) => _chunkSize = chunkSize;

        public int BytesRead { get; private set; }

        public override bool CanSeek => false;

        public override long Length => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = base.Read(buffer, offset, Math.Min(count, _chunkSize));
            BytesRead += read;
            return read;
        }
    }
}
