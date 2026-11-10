// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Azure.Functions.Worker.Tests.HostConfigurationDelivery
{
    internal sealed class FailingStream : TrickleStream
    {
        private readonly Exception _exception;

        public FailingStream() : this(new IOException("disk error at C:\\customer\\app"))
        {
        }

        public FailingStream(Exception exception) : base(new byte[10], int.MaxValue)
        {
            _exception = exception;
        }

        public override int Read(byte[] buffer, int offset, int count) => throw _exception;
    }
}
