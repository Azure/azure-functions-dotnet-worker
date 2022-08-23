// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    internal class SdkRetryOptions
    {
        public string? Strategy { get; set; }

        public int MaxRetryCount { get; set; }

        public string? DelayInterval { get; set; }

        public string? MinimumInterval { get; set; }

        public string? MaximumInterval { get; set; }
    }
}
