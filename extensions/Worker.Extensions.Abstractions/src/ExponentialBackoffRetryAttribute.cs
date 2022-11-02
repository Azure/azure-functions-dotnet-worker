// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Defines an exponential backoff retry strategy, where the delay between retries
    /// will get progressively larger, limited by the max/min specified.    
    /// </summary>
    public sealed class ExponentialBackoffRetryAttribute : RetryAttribute
    {
        /// <summary>
        /// Creates an instance of <see cref="ExponentialBackoffRetryAttribute"/>
        /// <param name="maxRetryCount">The maximum number of retries allowed per function execution</param>
        /// <param name="minimumInterval">The minimum retry delay. Specify as a string with the format HH:mm:ss</param>
        /// <param name="maximumInterval">The maximum retry delay. Specify as a string with the format HH:mm:ss.</param>
        /// </summary>
        public ExponentialBackoffRetryAttribute(int maxRetryCount, string minimumInterval, string maximumInterval)
        {
            if (!TimeSpan.TryParse(minimumInterval, out var parsedMinimumInterval))
            {
                throw new ArgumentOutOfRangeException(nameof(minimumInterval));
            }
            if (!TimeSpan.TryParse(maximumInterval, out var parsedMaximumInterval))
            {
                throw new ArgumentOutOfRangeException(nameof(maximumInterval));
            }

            ValidateIntervals(parsedMinimumInterval, parsedMaximumInterval);

            MaxRetryCount = maxRetryCount;
            MinimumInterval = minimumInterval;
            MaximumInterval = maximumInterval;
        }

        /// <summary>
        /// Gets the minimum retry delay, a string with format HH:mm:ss.
        /// </summary>
        public string MinimumInterval { get; }

        /// <summary>
        /// Gets the maximum retry delay, a string with format HH:mm:ss.
        /// </summary>
        public string MaximumInterval { get; }

        private static void ValidateIntervals(TimeSpan minimumInterval, TimeSpan maximumInterval)
        {
            if (minimumInterval.Ticks < 0)
            {
                throw new ArgumentOutOfRangeException("minimumInterval", "The TimeSpan must not be negative.");
            }

            if (maximumInterval.Ticks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumInterval), "The TimeSpan must not be negative.");
            }

            if (minimumInterval.Ticks > maximumInterval.Ticks)
            {
                throw new ArgumentException("The minimumInterval must not be greater than the maximumInterval.",
                    "minimumInterval");
            }
        }
    }
}
