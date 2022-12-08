// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Grpc.Messages
{
    internal sealed partial class StatusResult
    {
        private static StatusResult? _successInstance;

        public StatusResult(Types.Status status)
        {
            Status = status;
        }

        public static StatusResult Success => _successInstance ??= new StatusResult(Types.Status.Success);
    }
}
