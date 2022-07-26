using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Functions.Worker
{
    public class FixedDelayRetryAttribute : RetryAttribute
    {
        public const string Strategy = "fixedDelay";

        public FixedDelayRetryAttribute(int maxRetryCount, string delayInterval)
        {
            MaxRetryCount = maxRetryCount;
            DelayInterval = delayInterval;
        }

        public int MaxRetryCount { get; }

        public string DelayInterval { get; }
    }
}
