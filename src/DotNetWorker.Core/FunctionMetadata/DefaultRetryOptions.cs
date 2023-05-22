// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata
{
    /// <summary>
    /// Function execution retry policy to use on invocation failures.
    /// </summary>
    public class DefaultRetryOptions : IRetryOptions
    {
        /// <inheritdoc/>
        public int MaxRetryCount { get; set; }

        /// <inheritdoc/>
        public TimeSpan? DelayInterval { get; set; }

        /// <inheritdoc/>
        public TimeSpan? MinimumInterval { get; set; }

        /// <inheritdoc/>
        public TimeSpan? MaximumInterval { get; set; }

        /// <inheritdoc/>
        public RetryStrategy? Strategy => DelayInterval is null ? RetryStrategy.ExponentialBackoff : RetryStrategy.FixedDelay;
    }
}
