// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class BlobFunction
    {
        [Function("BlobFunction")]
        [BlobOutput("test-samples-output/{name}-output.txt")]
        public static string Run(
            [BlobTrigger("test-samples-trigger/{name}")] string myTriggerItem,
            [BlobInput("test-samples-input/sample1.txt")] string myBlob,
            FunctionContext context)
        {
            var logger = context.GetLogger("BlobFunction");
            logger.LogInformation($"Triggered Item = {myTriggerItem}");
            logger.LogInformation($"Input Item = {myBlob}");

            // Blob Output
            return "blob-output content";
        }
    }
}
