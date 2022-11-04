// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class BlobTriggerBindingSamples
    {
        [Function(nameof(BlobClientFunction))]
        public static async Task BlobClientFunction(
            [BlobTrigger("client-trigger/{name}", Connection = "AzureWebJobsStorage")] BlobClient client,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(BlobClientFunction));
            var downloadResult = await client.DownloadContentAsync();
            var content = downloadResult.Value.Content.ToString();
            logger.LogInformation("Blob content: {content}", content);
        }

        [Function(nameof(BlobStreamFunction))]
        public static void BlobStreamFunction(
            [BlobTrigger("stream-trigger/{name}", Connection = "AzureWebJobsStorage")] Stream stream,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(BlobStreamFunction));
            using var blobStreamReader = new StreamReader(stream);
            logger.LogInformation("Blob content: {stream}", blobStreamReader.ReadToEnd());
        }

        [Function(nameof(BlobByteArrayFunction))]
        public static void BlobByteArrayFunction(
            [BlobTrigger("byte-trigger/{name}", Connection = "AzureWebJobsStorage")] Byte[] data,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(BlobByteArrayFunction));
            logger.LogInformation($"Blob content: {Encoding.Default.GetString(data)}");
        }

        [Function(nameof(BlobStringFunction))]
        public static void BlobStringFunction(
            [BlobTrigger("string-trigger/{name}", Connection = "AzureWebJobsStorage")] string data,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(BlobStringFunction));
            logger.LogInformation($"Blob content: {data}");
        }

        [Function(nameof(BlobBookFunction))]
        public static void BlobBookFunction(
            [BlobTrigger("book-trigger/{name}", Connection = "AzureWebJobsStorage")] Book data,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(BlobBookFunction));
            logger.LogInformation($"Id: {data.Id} - Name: {data.Name}");
        }
    }
}
