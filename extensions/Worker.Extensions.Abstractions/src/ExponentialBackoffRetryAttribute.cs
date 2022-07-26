using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Functions.Worker
{
    public class ExponentialBackoffRetryAttribute : RetryAttribute
    {
        public const string Strategy = "exponentialBackoff";

        public ExponentialBackoffRetryAttribute(int maxRetryCount, string minimumInterval, string maximumInterval)
        {
            MaxRetryCount = maxRetryCount;
            MinimumInterval = minimumInterval;
            MaximumInterval = maximumInterval;
        }

        public int MaxRetryCount { get; }

        public string MinimumInterval { get; }

        public string MaximumInterval { get; }
    }
}
