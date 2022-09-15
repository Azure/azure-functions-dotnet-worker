// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class BlobBookFunction
    {
        [Function(nameof(BlobBookFunction))]
        public static async Task Run(
            [BlobTrigger("book-trigger", Connection = "AzureWebJobsStorage")] Book book,
            [BlobInput("book-input/{id}.txt", Connection = "AzureWebJobsStorage")] string myBlob,
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
