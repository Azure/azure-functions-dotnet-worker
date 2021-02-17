// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿ using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Storage;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class BlobFunction
    {
        [FunctionName("BlobFunction")]
        [BlobOutput("output", "test-samples-output/{name}-output.txt", Connection = "AzureWebJobsStorage")]
        public static void Run(
            [BlobTrigger("test-samples-trigger/{name}", Connection = "AzureWebJobsStorage")] string myTriggerItem,
            [BlobInput("test-samples-input/sample1.txt", Connection = "AzureWebJobsStorage")] string myBlob,
            FunctionExecutionContext context)
        {
            var logger = context.Logger;
            logger.LogInformation($"Triggered Item = {myTriggerItem}");
            logger.LogInformation($"Input Item = {myBlob}");

            // Blob Output
            context.OutputBindings["output"] = "queue message";
        }
    }
}
