// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Attribute used to mark a function that has an FixedDelay retry policy.
    /// </summary>
    public class FixedDelayRetryAttribute : RetryAttribute
    {
        /// <summary>
        /// The type of retry policy.
        /// </summary>
        public const string Strategy = "fixedDelay";

        /// <summary>
        /// Creates an instance of <see cref="FixedDelayRetryAttribute"/>
        /// </summary>
        /// <param name="maxRetryCount">The maximum number of retries allowed per function execution</param>
        /// <param name="delayInterval">The delay that is used between retries. Specify as a string with the format HH:mm:ss</param>
        public FixedDelayRetryAttribute(int maxRetryCount, string delayInterval)
        {
            MaxRetryCount = maxRetryCount;
            DelayInterval = delayInterval;
        }

        /// <summary>
        /// Get the maximum number of retries allowed per function execution.
        /// </summary>
        public int MaxRetryCount { get; }

        /// <summary>
        /// Get the delay used between retries, a string with the format HH:mm:ss
        /// </summary>
        public string DelayInterval { get; }
    }
}
