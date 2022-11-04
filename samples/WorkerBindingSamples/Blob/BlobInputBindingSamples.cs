// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public class BlobInputBindingSamples
    {
        private readonly ILogger<BlobInputBindingSamples> _logger;

        public BlobInputBindingSamples(ILogger<BlobInputBindingSamples> logger)
        {
            _logger = logger;
        }

        [Function(nameof(BlobInputClientFunction))]
        public async Task<HttpResponseData> BlobInputClientFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [BlobInput("input-container/sample1.txt", Connection = "AzureWebJobsStorage")] BlobClient client)
        {
            var downloadResult = await client.DownloadContentAsync();
            var content = downloadResult.Value.Content.ToString();
            _logger.LogInformation("Blob content: {content}", content);

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(BlobInputStreamFunction))]
        public HttpResponseData BlobInputStreamFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [BlobInput("input-container/sample1.txt", Connection = "AzureWebJobsStorage")] Stream stream)
        {
            using var blobStreamReader = new StreamReader(stream);
            _logger.LogInformation("Blob content: {stream}", blobStreamReader.ReadToEnd());

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(BlobInputByteArrayFunction))]
        public HttpResponseData BlobInputByteArrayFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [BlobInput("input-container/sample1.txt", Connection = "AzureWebJobsStorage")] Byte[] data)
        {
            _logger.LogInformation($"Blob content: {Encoding.Default.GetString(data)}");
            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(BlobInputStringFunction))]
        public HttpResponseData BlobInputStringFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [BlobInput("input-container/sample1.txt", Connection = "AzureWebJobsStorage")] string data)
        {
            _logger.LogInformation($"Blob content: {data}");
            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(BlobInputBookFunction))]
        public HttpResponseData BlobInputBookFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [BlobInput("input-container/book.json", Connection = "AzureWebJobsStorage")] Book data)
        {
            _logger.LogInformation($"Book name: {data.Name}");
            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
