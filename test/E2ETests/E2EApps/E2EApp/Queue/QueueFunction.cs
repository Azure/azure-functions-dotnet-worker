// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Storage;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp.Queue
{
    public class QueueFunction
    {
        [FunctionName("QueueTrigger")]
        [QueueOutput("outQueue", "test-output-node")]
        public static void Run([QueueTrigger("test-input-node")] string message,
            FunctionContext context)
        {
            var logger = context.GetLogger<QueueFunction>();
            logger.LogInformation($"Message: {message}");

            context.OutputBindings["outQueue"] = message;
        }
    }
}
