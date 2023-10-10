// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace FunctionsNetHost.Grpc
{
    public sealed class WorkerDescription
    {
        public string? DefaultWorkerPath { set; get; }

        public bool IsSpecializable { set; get; }
    }
}
