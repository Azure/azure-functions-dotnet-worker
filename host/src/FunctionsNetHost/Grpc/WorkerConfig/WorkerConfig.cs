// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace FunctionsNetHost.Grpc
{
    /// <summary>
    /// Represents a worker configuration instance.
    /// </summary>
    public sealed class WorkerConfig
    {
        public WorkerDescription? Description { set; get; }
    }
}
