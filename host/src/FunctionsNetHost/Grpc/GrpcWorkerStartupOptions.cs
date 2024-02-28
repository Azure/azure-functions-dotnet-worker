﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace FunctionsNetHost.Grpc
{
    internal sealed class GrpcWorkerStartupOptions
    {
        public Uri ServerUri { get; set; }

        public string? WorkerId { get; set; }

        public string? RequestId { get; set; }

        public int GrpcMaxMessageLength { get; set; }

        public string[] CommandLineArgs { get; set; }
    }
}
