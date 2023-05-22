// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Grpc.Messages
{
    internal sealed partial class RpcFunctionMetadata : IFunctionMetadata
    {
        IList<string> IFunctionMetadata.RawBindings => RawBindings;

        IRetryOptions IFunctionMetadata.Retry => RetryOptions;
    }
}
