// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Warmup;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class Warmup
    {
        [FunctionName("Warmup")]
        public static void Run([WarmupTrigger] object _, FunctionExecutionContext context)
        {
            var logger = context.Logger;

            logger.LogInformation("Function App instance is now warm!");
        }
    }
}
