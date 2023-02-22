// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp.Blob
{
    public class BlobInputBindingFunctions
    {
        private readonly ILogger<BlobInputBindingFunctions> _logger;

        public BlobInputBindingFunctions(ILogger<BlobInputBindingFunctions> logger)
        {
            _logger = logger;
        }

        [Function(nameof(BlobInputClientTest))]
        public async Task<HttpResponseData> BlobInputClientTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated/testFile.txt")] BlobClient client)
        {
            var downloadResult = await client.DownloadContentAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.Body.WriteAsync(downloadResult.Value.Content);
            return response;
        }

        [Function(nameof(BlobInputContainerClientTest))]
        public async Task<HttpResponseData> BlobInputContainerClientTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated/testFile.txt")] BlobContainerClient client)
        {
            var blobClient = client.GetBlobClient("testFile.txt");
            var downloadResult = await blobClient.DownloadContentAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.Body.WriteAsync(downloadResult.Value.Content);
            return response;
        }

        [Function(nameof(BlobInputStreamTest))]
        public async Task<HttpResponseData> BlobInputStreamTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated/testFile.txt")] Stream stream)
        {
            using var blobStreamReader = new StreamReader(stream);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(blobStreamReader.ReadToEnd());
            return response;
        }

        [Function(nameof(BlobInputByteTest))]
        public async Task<HttpResponseData> BlobInputByteTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated/testFile.txt")] Byte[] data)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(Encoding.Default.GetString(data));
            return response;
        }

        [Function(nameof(BlobInputStringTest))]
        public async Task<HttpResponseData> BlobInputStringTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated/testFile.txt")] string data)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(data);
            return response;
        }

        [Function(nameof(BlobInputPocoTest))]
        public async Task<HttpResponseData> BlobInputPocoTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated/testFile.txt")] Book data)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(data.Name);
            return response;
        }

        [Function(nameof(BlobInputCollectionTest))]
        public async Task<HttpResponseData> BlobInputCollectionTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated", IsBatched = true)] IEnumerable<BlobClient> blobs)
        {
            List<string> blobList = new();

            foreach (BlobClient blob in blobs)
            {
                _logger.LogInformation("Blob name: {blobName}, Container name: {containerName}", blob.Name, blob.BlobContainerName);
                blobList.Add(blob.Name);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(blobList.ToString());
            return response;
        }

        [Function(nameof(BlobInputStringArrayTest))]
        public async Task<HttpResponseData> BlobInputStringArrayTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated", IsBatched = true)] string[] blobContent)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(blobContent.ToString());
            return response;
        }

        [Function(nameof(BlobInputPocoArrayTest))]
        public async Task<HttpResponseData> BlobInputPocoArrayTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated", IsBatched = true)] Book[] books)
        {
            List<string> bookNames = new();

            foreach (var item in books)
            {
                bookNames.Add(item.Name);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(bookNames.ToString());
            return response;
        }
    }
}