// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class Warmup
    {
        [Function("Warmup")]
        public static void Run([WarmupTrigger] object warmupContext, FunctionContext context)
        {
            var logger = context.GetLogger("Warmup");

            logger.LogInformation("Function App instance is now warm!");
        }
    }
}
