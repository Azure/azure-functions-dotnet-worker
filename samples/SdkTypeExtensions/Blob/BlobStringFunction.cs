// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class BlobStringFunction
    {
        [Function(nameof(BlobStringFunction))]
        public static async Task Run(
            [BlobTrigger("blobstring-trigger/{name}", Connection = "AzureWebJobsStorage")] string data,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(BlobStringFunction));
            logger.LogInformation($"Blob content: {data}");
        }
    }
}
