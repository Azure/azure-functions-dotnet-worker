// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Grpc.Messages
{
    internal sealed partial class RpcRetryOptions : IRetryOptions
    {
        int? IRetryOptions.MaxRetryCount => MaxRetryCount;

        string? IRetryOptions.DelayInterval => DelayInterval.ToTimeSpan().ToString();

        string? IRetryOptions.MinimumInterval => MinimumInterval.ToTimeSpan().ToString();

        string? IRetryOptions.MaximumInterval => MaximumInterval.ToString();

        string IRetryOptions.Strategy => RetryStrategy.ToString();
    }
}
