// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class ExpressionFunction
    {
        [Function(nameof(ExpressionFunction))]
        public static void Run(
            [QueueTrigger("expression-trigger")] Book book,
            [BlobInput("input-container/{id}.txt")] string myBlob,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(ExpressionFunction));
            logger.LogInformation("Trigger content: {content}", book);
            logger.LogInformation("Blob content: {content}", myBlob);
        }
    }
}
