// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Defines a retry strategy where a fixed delay is used between retries.
    /// </summary>
    public sealed class FixedDelayRetryAttribute : RetryAttribute
    {
        /// <summary>
        /// Creates an instance of <see cref="FixedDelayRetryAttribute"/>
        /// </summary>
        /// <param name="maxRetryCount">The maximum number of retries allowed per function execution</param>
        /// <param name="delayInterval">The delay that is used between retries. Specify as a string with the format HH:mm:ss</param>
        public FixedDelayRetryAttribute(int maxRetryCount, string delayInterval)
        {
            if (!TimeSpan.TryParse(delayInterval, out TimeSpan parsedDelayInterval))
            {
                throw new ArgumentOutOfRangeException(nameof(delayInterval));
            }

            ValidateInterval(parsedDelayInterval);

            MaxRetryCount = maxRetryCount;
            DelayInterval = delayInterval;
        }

        /// <summary>
        /// Get the delay used between retries, a string with the format HH:mm:ss
        /// </summary>
        public string DelayInterval { get; }

        private static void ValidateInterval(TimeSpan delayInterval)
        {
            if (delayInterval.Ticks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(delayInterval), "The TimeSpan must not be negative.");
            }
        }
    }
}
