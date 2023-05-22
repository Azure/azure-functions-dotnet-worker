// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Grpc.Messages
{
    internal sealed partial class RpcRetryOptions : IRetryOptions
    {
        int IRetryOptions.MaxRetryCount => MaxRetryCount;

        TimeSpan? IRetryOptions.DelayInterval => DelayInterval.ToTimeSpan();

        TimeSpan? IRetryOptions.MinimumInterval => MinimumInterval.ToTimeSpan();

        TimeSpan? IRetryOptions.MaximumInterval => MaximumInterval.ToTimeSpan();

        RetryStrategy? IRetryOptions.Strategy => RetryStrategy switch
        {
            Types.RetryStrategy.FixedDelay => Core.FunctionMetadata.RetryStrategy.FixedDelay,
            Types.RetryStrategy.ExponentialBackoff => Core.FunctionMetadata.RetryStrategy.ExponentialBackoff,
            _ => throw new InvalidOperationException($"Unknown RpcDataType: {RetryStrategy}")
        };
    }
}
