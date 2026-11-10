// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Azure.Functions.Worker.Tests.HostConfigurationDelivery
{
    // Seekable stream that reports a small Length but keeps producing data, like a file that grows while read.
    internal sealed class UnderReportingStream : Stream
    {
        private readonly int _total;
        private int _served;

        public UnderReportingStream(int total) => _total = total;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => 10;
        public override long Position { get => _served; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            int n = Math.Min(count, _total - _served);
            for (int i = 0; i < n; i++)
            {
                buffer[offset + i] = (byte)'a';
            }

            _served += n;
            return n;
        }
    }
}
