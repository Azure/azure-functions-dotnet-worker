// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System;

namespace Microsoft.Azure.Functions.Worker
{
    internal class GrpcWorkerStartupOptions
    {
        public Uri? Uri { get; set; }

        public string? WorkerId { get; set; }

        public string? RequestId { get; set; }

        public int GrpcMaxMessageLength { get; set; }
    }
}
