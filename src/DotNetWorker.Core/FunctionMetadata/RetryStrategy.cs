// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata
{
    /// <summary>
    /// The retry strategy used in the case of function invocation failure.
    /// </summary>
    public enum RetryStrategy
    {
        /// <summary>
        /// Exponential backoff strategy. The first retry waits for the minimum delay. On subsequent retries, time is added exponentially to the initial duration for each retry, until the maximum delay is reached.
        /// </summary>
        ExponentialBackoff,
        /// <summary>
        /// A fixed delay strategy.A specified amount of time is allowed to elapse between each retry.
        /// </summary>
        FixedDelay
    }
}
