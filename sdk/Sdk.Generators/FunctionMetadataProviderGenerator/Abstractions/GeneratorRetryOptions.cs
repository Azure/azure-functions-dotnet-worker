﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    internal class GeneratorRetryOptions
    {
        public RetryStrategy Strategy { get; set; }

        public string? MaxRetryCount { get; set; }

        public string? DelayInterval { get; set; }

        public string? MinimumInterval { get; set; }

        public string? MaximumInterval { get; set; }
    }
}
