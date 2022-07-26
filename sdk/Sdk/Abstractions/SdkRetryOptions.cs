using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    internal class SdkRetryOptions
    {
        public SdkRetryOptions(string strategy, int maxRetryCount, string? delayInterval, string? minimumInterval, string? maximumInterval)
        {
            Strategy = strategy;
            MaxRetryCount = maxRetryCount;
            DelayInterval = delayInterval;
            MinimumInterval = minimumInterval;
            MaximumInterval = maximumInterval;
        }

        public string Strategy { get; set; }

        public int MaxRetryCount { get; set; }

        public string? DelayInterval { get; set; }

        public string? MinimumInterval { get; set; }

        public string? MaximumInterval { get; set; }
    }
}
