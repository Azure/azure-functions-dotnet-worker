// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

// This scenario does not currently work
namespace SampleApp
{
    public static class BlobBookFunction
    {
        [Function(nameof(BlobBookFunction))]
        public static void Run(
            [BlobTrigger("book-trigger", Connection = "AzureWebJobsStorage")] Book book,
            [BlobInput("input-container/{id}.txt", Connection = "AzureWebJobsStorage")] string myBlob,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(BlobBookFunction));
            logger.LogInformation("Trigger content: {content}", book);
            logger.LogInformation("Blob content: {content}", myBlob);
        }
    }

    public class Book
    {
        public string Name { get; set; }
        public string Id { get; set; }
    }
}
