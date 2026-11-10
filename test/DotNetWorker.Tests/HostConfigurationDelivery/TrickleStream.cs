// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Azure.Functions.Worker.Tests.HostConfigurationDelivery
{
    internal class TrickleStream : Stream
    {
        private readonly byte[] _data;
        private readonly long _maxBytes;
        private readonly bool _endless;
        private long _position;

        public TrickleStream(byte[] data, long maxBytes, bool endless = false)
        {
            _data = data;
            _maxBytes = maxBytes;
            _endless = endless;
        }

        public long TotalServed => _position;

        public bool Disposed { get; private set; }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0 || (!_endless && _position >= _data.Length) || _position >= _maxBytes)
            {
                return 0;
            }

            // Return a single byte per call to exercise the read loop.
            buffer[offset] = _endless ? (byte)'x' : _data[_position];
            _position++;
            return 1;
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
