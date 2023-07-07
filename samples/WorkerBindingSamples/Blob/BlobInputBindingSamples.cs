// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net;
using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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

        [Function(nameof(BlobInputContainerClientFunction))]
        public async Task<HttpResponseData> BlobInputContainerClientFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("input-container")] BlobContainerClient client)
        {
            _logger.LogInformation("Blobs within container:");

            var resultSegment = client.GetBlobsAsync();
            await foreach (BlobItem blob in resultSegment)
            {
                _logger.LogInformation(blob.Name);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(BlobInputClientFunction))]
        public async Task<HttpResponseData> BlobInputClientFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
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
        public async Task<HttpResponseData> BlobInputCollectionFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("input-container")] IEnumerable<BlobClient> blobs)
        {
            _logger.LogInformation("Content of all blobs within container:");

            foreach (BlobClient blob in blobs)
            {
                var downloadResult = await blob.DownloadContentAsync();
                var content = downloadResult.Value.Content.ToString();
                _logger.LogInformation(content);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(BlobInputCollectionSubdirectoryFunction))]
        public async Task<HttpResponseData> BlobInputCollectionSubdirectoryFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("input-container/test")] IEnumerable<BlobClient> blobs)
        {
            _logger.LogInformation("Content of all blobs within the 'test' subdirectory in the container:");

            foreach (BlobClient blob in blobs)
            {
                var downloadResult = await blob.DownloadContentAsync();
                var content = downloadResult.Value.Content.ToString();
                _logger.LogInformation(content);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(BlobInputStreamArrayFunction))]
        public async Task<HttpResponseData> BlobInputStreamArrayFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("input-container")] Stream[] blobContent)
        {
            _logger.LogInformation("Content of all blobs within container:");

            foreach (var item in blobContent)
            {
                using var blobStreamReader = new StreamReader(item);
                _logger.LogInformation(await blobStreamReader.ReadToEndAsync());
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(BlobInputBytesArrayFunction))]
        public HttpResponseData BlobInputBytesArrayFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("input-container")] byte[][] blobContent)
        {
            _logger.LogInformation("Content of all blobs within container:");

            foreach (var item in blobContent)
            {
                _logger.LogInformation(Encoding.Default.GetString(item));
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(BlobInputStringArrayFunction))]
        public HttpResponseData BlobInputStringArrayFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("input-container")] string[] blobContent)
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
            [BlobInput("input-container")] Book[] books)
        {
            _logger.LogInformation("Content of all blobs within container:");

            foreach (Book book in books)
            {
                _logger.LogInformation(book.Name);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
