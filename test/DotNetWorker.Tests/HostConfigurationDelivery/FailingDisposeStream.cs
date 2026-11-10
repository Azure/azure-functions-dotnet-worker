// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;
using System.Text;

namespace Microsoft.Azure.Functions.Worker.Tests.HostConfigurationDelivery
{
    internal sealed class FailingDisposeStream : MemoryStream
    {
        public FailingDisposeStream() : base(Encoding.UTF8.GetBytes("{}"))
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            throw new FileNotFoundException();
        }
    }
}
