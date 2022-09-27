// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class BlobClientFunction
    {
        [Function(nameof(BlobClientFunction))]
        public static async Task Run(
            [BlobTrigger("blobclient-trigger/{name}", Connection = "AzureWebJobsStorage")] BlobClient client,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(BlobClientFunction));
            var downloadResult = await client.DownloadContentAsync();
            var content = downloadResult.Value.Content.ToString();
            logger.LogInformation("Blob content: {content}", content);
        }
    }
}
