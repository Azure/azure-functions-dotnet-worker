// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Warmup;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class Warmup
    {
        [Function("Warmup")]
        public static void Run([WarmupTrigger] object _, FunctionContext context)
        {
            var logger = context.GetLogger("Warmup");

            logger.LogInformation("Function App instance is now warm!");
        }
    }
}
