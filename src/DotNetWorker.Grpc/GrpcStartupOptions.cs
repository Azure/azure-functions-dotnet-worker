// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


namespace Microsoft.Azure.Functions.Worker
{
    internal class GrpcWorkerStartupOptions
    {
        /// <summary>
        /// Absolute URI of the grpc server including port.
        /// Use this value to connect if not null, else use <see cref="Host"/> and <see cref="Port"/> to build the URI for connecting to the grpc server.
        /// </summary>
        public string? Uri { get; set; }

        public string? Host { get; set; }

        public int Port { get; set; }

        public string? WorkerId { get; set; }

        public string? RequestId { get; set; }

        public int GrpcMaxMessageLength { get; set; }
    }
}
