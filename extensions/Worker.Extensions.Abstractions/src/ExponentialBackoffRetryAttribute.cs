// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Attribute used to mark a function that has an ExpontentialBackoff retry policy.
    /// </summary>
    public class ExponentialBackoffRetryAttribute : RetryAttribute
    {
        /// <summary>
        /// The type of retry policy.
        /// </summary>
        public const string Strategy = "exponentialBackoff";

        /// <summary>
        /// Creates an instance of <see cref="ExponentialBackoffRetryAttribute"/>
        /// <param name="maxRetryCount">The maximum number of retries allowed per function execution</param>
        /// <param name="minimumInterval">The minimum retry delay. Specify as a string with the format HH:mm:ss</param>
        /// <param name="maximumInterval">The maximum retry delay. Specify as a string with the format HH:mm:ss.</param>
        /// </summary>
        public ExponentialBackoffRetryAttribute(int maxRetryCount, string minimumInterval, string maximumInterval)
        {
            MaxRetryCount = maxRetryCount;
            MinimumInterval = minimumInterval;
            MaximumInterval = maximumInterval;
        }

        /// <summary>
        /// Gets the maximum number of retries allowed per function execution.
        /// </summary>
        public int MaxRetryCount { get; }

        /// <summary>
        /// Gets the minimum retry delay, a string with format HH:mm:ss.
        /// </summary>
        public string MinimumInterval { get; }

        /// <summary>
        /// Gets the maximum retry delay, a string with format HH:mm:ss.
        /// </summary>
        public string MaximumInterval { get; }
    }
}
