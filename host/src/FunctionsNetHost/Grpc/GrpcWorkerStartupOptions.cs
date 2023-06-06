// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace FunctionsNetHost.Grpc
{
    internal sealed class GrpcWorkerStartupOptions
    {
        public string? Host { get; set; }

        public int Port { get; set; }

        public string? WorkerId { get; set; }

        public string? RequestId { get; set; }

        public int GrpcMaxMessageLength { get; set; }

        public override string ToString() => $"Host:{Host}, Port:{Port}, WorkerId:{WorkerId}, RequestId:{RequestId}, GrpcMaxMessageLength:{GrpcMaxMessageLength}";
    }
}
