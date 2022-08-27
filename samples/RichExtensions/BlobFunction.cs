// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class BlobFunction
    {
        [Function(nameof(BlobFunction))]
        public static async Task Run(
            [BlobTrigger("test-trigger/{name}")] BlobClient client,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(BlobFunction));
            var downloadResult = await client.DownloadContentAsync();
            var content = downloadResult.Value.Content.ToString();
            logger.LogInformation($"Blob content: {content}");
        }
    }
}
