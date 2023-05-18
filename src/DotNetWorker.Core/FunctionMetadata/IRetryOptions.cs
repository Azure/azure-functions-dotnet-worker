// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata
{
    /// <summary>
    /// Function execution retry policy to use on invocation failures.
    /// </summary>
    public interface IRetryOptions
    {
        /// <summary>
        /// The maximum number of retries allowed per function execution. -1 means to retry indefinitely.
        /// </summary>
        public int? MaxRetryCount { get; }

        /// <summary>
        /// The delay used between retries when using a fixed delay strategy.
        /// </summary>
        public string? DelayInterval { get; }

        /// <summary>
        /// The minimum retry delay when using an exponential backoff strategy.
        /// </summary>
        public string? MinimumInterval { get; }

        /// <summary>
        /// The maximum retry delay when using an exponential backoff strategy.
        /// </summary>
        public string? MaximumInterval { get; }

        /// <summary>
        /// The retry strategy being used (fixed delay or exponential backoff).
        /// </summary>
        public string? Strategy { get; }
    }
}
