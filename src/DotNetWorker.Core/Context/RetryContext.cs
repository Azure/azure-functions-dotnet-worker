// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Exposes information about retry activity for the event that triggered
    /// the current function invocation.
    /// </summary>
    public abstract class RetryContext
    {
        /// <summary>
        /// The the retry count for the current event.
        /// </summary>
        public abstract int RetryCount { get; }

        /// <summary>
        /// The maximum number of retry attempts that will be made by the trigger/host
        /// before the event is considered undeliverable.
        /// </summary>
        public abstract int MaxRetryCount { get; }

        /// <summary>
        /// Gets diagnostic information about the remote exception that caused the preceding
        /// invocation to be retried, or <see langword="null"/> when none was supplied.
        /// </summary>
        /// <remarks>
        /// The returned value is remote diagnostic data and is not a locally throwable exception.
        /// </remarks>
        public virtual FunctionRetryException? PreviousException => null;
    }
}
