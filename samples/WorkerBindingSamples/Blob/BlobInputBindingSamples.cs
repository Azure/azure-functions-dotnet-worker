// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net;
using System.Text;
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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [BlobInput("input-container/sample1.txt")] BlobClient client)
        {
            var downloadResult = await client.DownloadContentAsync();
            var content = downloadResult.Value.Content.ToString();

            _logger.LogInformation("Blob content: {content}", content);

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(BlobInputStreamFunction))]
        public async Task<HttpResponseData> BlobInputStreamFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("input-container/sample1.txt")] Stream stream)
        {
            using var blobStreamReader = new StreamReader(stream);
            _logger.LogInformation("Blob content: {content}", await blobStreamReader.ReadToEndAsync());

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(BlobInputByteArrayFunction))]
        public HttpResponseData BlobInputByteArrayFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("input-container/sample1.txt")] Byte[] data)
        {
            _logger.LogInformation("Blob content: {content}", Encoding.Default.GetString(data));
            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(BlobInputStringFunction))]
        public HttpResponseData BlobInputStringFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, string filename,
            [BlobInput("input-container/{filename}")] string data)
        {
            _logger.LogInformation("Blob content: {content}", data);
            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(BlobInputBookFunction))]
        public HttpResponseData BlobInputBookFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("input-container/book.json")] Book data)
        {
            _logger.LogInformation("Book name: {name}", data.Name);
            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(BlobInputCollectionFunction))]
        public HttpResponseData BlobInputCollectionFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("input-container", IsBatched = true)] IEnumerable<BlobClient> blobs)
        {
            _logger.LogInformation("Blobs within container:");

            foreach (BlobClient blob in blobs)
            {
                _logger.LogInformation("Blob name: {blobName}, Container name: {containerName}", blob.Name, blob.BlobContainerName);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(BlobInputStringArrayFunction))]
        public HttpResponseData BlobInputStringArrayFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("input-container", IsBatched = true)] string[] blobContent)
        {
            _logger.LogInformation("Content of all blobs within container:");

            foreach (var item in blobContent)
            {
                _logger.LogInformation(item);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(BlobInputBookArrayFunction))]
        public HttpResponseData BlobInputBookArrayFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("input-container", IsBatched = true)] Book[] books)
        {
            _logger.LogInformation("Content of all blobs within container:");

            foreach (var item in books)
            {
                _logger.LogInformation(item.Name);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
