// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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

        public string Strategy { get; }

        public int MaxRetryCount { get; }

        public string? DelayInterval { get; }

        public string? MinimumInterval { get; }

        public string? MaximumInterval { get; }
    }
}
