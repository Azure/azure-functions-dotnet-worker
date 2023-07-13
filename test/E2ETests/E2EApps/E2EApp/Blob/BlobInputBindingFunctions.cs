// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
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

        [Function(nameof(BlobInputBlockClientTest))]
        public async Task<HttpResponseData> BlobInputBlockClientTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated/testFile.txt")] BlockBlobClient client)
        {
            var downloadResult = await client.DownloadContentAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.Body.WriteAsync(downloadResult.Value.Content);
            return response;
        }

        [Function(nameof(BlobInputAppendClientTest))]
        public async Task<HttpResponseData> BlobInputAppendClientTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated/testFile.txt")] AppendBlobClient client)
        {
            var downloadResult = await client.DownloadContentAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.Body.WriteAsync(downloadResult.Value.Content);
            return response;
        }

        [Function(nameof(BlobInputPageClientTest))]
        public async Task<HttpResponseData> BlobInputPageClientTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated/testFile.txt")] PageBlobClient client)
        {
            var downloadResult = await client.DownloadContentAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.Body.WriteAsync(downloadResult.Value.Content);
            return response;
        }

        [Function(nameof(BlobInputBaseClientTest))]
        public async Task<HttpResponseData> BlobInputBaseClientTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated/testFile.txt")] BlobBaseClient client)
        {
            var downloadResult = await client.DownloadContentAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.Body.WriteAsync(downloadResult.Value.Content);
            return response;
        }

        [Function(nameof(BlobInputContainerClientTest))]
        public async Task<HttpResponseData> BlobInputContainerClientTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated")] BlobContainerClient client)
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

        [Function(nameof(BlobInputClientArrayTest))]
        public async Task<HttpResponseData> BlobInputClientArrayTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated")] BlobClient[] blobs)
        {
            List<string> blobList = new();

            foreach (BlobClient blob in blobs)
            {
                var downloadResult = await blob.DownloadContentAsync();
                var content = downloadResult.Value.Content.ToString();
                blobList.Add(content);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            string contentAsString = string.Join(", ", blobList.ToArray());
            await response.WriteStringAsync(contentAsString);
            return response;
        }

        [Function(nameof(BlobInputStreamArrayTest))]
        public async Task<HttpResponseData> BlobInputStreamArrayTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated")] Stream[] blobContent)
        {
            List<string> blobList = new();

            foreach (Stream stream in blobContent)
            {
                using var blobStreamReader = new StreamReader(stream);
                blobList.Add(blobStreamReader.ReadToEnd());
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            string contentAsString = string.Join(", ", blobList.ToArray());
            await response.WriteStringAsync(contentAsString);
            return response;
        }

        [Function(nameof(BlobInputBytesArrayTest))]
        public async Task<HttpResponseData> BlobInputBytesArrayTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated")] byte[][] blobContent)
        {
            List<string> blobList = new();

            foreach (byte[] bytes in blobContent)
            {
                blobList.Add(Encoding.Default.GetString(bytes));
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            string contentAsString = string.Join(", ", blobList.ToArray());
            await response.WriteStringAsync(contentAsString);
            return response;
        }

        [Function(nameof(BlobInputBytesArraySingleBlobTest))]
        public async Task<HttpResponseData> BlobInputBytesArraySingleBlobTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated/testFile.txt")] byte[][] blobContent)
        {
            List<string> blobList = new();

            foreach (byte[] bytes in blobContent)
            {
                blobList.Add(Encoding.Default.GetString(bytes));
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            string contentAsString = string.Join(", ", blobList.ToArray());
            await response.WriteStringAsync(contentAsString);
            return response;
        }

        [Function(nameof(BlobInputStringArrayTest))]
        public async Task<HttpResponseData> BlobInputStringArrayTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated")] string[] blobContent)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            string contentAsString = string.Join(", ", blobContent);
            await response.WriteStringAsync(contentAsString);
            return response;
        }

        [Function(nameof(BlobInputStringArraySingleBlobTest))]
        public async Task<HttpResponseData> BlobInputStringArraySingleBlobTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated/testFile.txt")] string[] blobContent)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            string contentAsString = string.Join(", ", blobContent);
            await response.WriteStringAsync(contentAsString);
            return response;
        }

        [Function(nameof(BlobInputPocoArrayTest))]
        public async Task<HttpResponseData> BlobInputPocoArrayTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated")] Book[] books)
        {
            List<string> bookNames = new();

            foreach (var item in books)
            {
                bookNames.Add(item.Name);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            string contentAsString = string.Join(", ", bookNames.ToArray());
            await response.WriteStringAsync(contentAsString);
            return response;
        }

        [Function(nameof(BlobInputPocoArraySingleBlobTest))]
        public async Task<HttpResponseData> BlobInputPocoArraySingleBlobTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated/testFile.txt")] Book[] books)
        {
            List<string> bookNames = new();

            foreach (var item in books)
            {
                bookNames.Add(item.Name);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            string contentAsString = string.Join(", ", bookNames.ToArray());
            await response.WriteStringAsync(contentAsString);
            return response;
        }

        [Function(nameof(BlobInputClientEnumerableTest))]
        public async Task<HttpResponseData> BlobInputClientEnumerableTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated")] IEnumerable<BlobClient> blobs)
        {
            List<string> blobList = new();

            foreach (BlobClient blob in blobs)
            {
                var downloadResult = await blob.DownloadContentAsync();
                var content = downloadResult.Value.Content.ToString();
                blobList.Add(content);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            string contentAsString = string.Join(", ", blobList.ToArray());
            await response.WriteStringAsync(contentAsString);
            return response;
        }

        [Function(nameof(BlobInputStreamEnumerableTest))]
        public async Task<HttpResponseData> BlobInputStreamEnumerableTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated")] IEnumerable<Stream> blobContent)
        {
            List<string> blobList = new();

            foreach (Stream stream in blobContent)
            {
                using var blobStreamReader = new StreamReader(stream);
                blobList.Add(blobStreamReader.ReadToEnd());
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            string contentAsString = string.Join(", ", blobList.ToArray());
            await response.WriteStringAsync(contentAsString);
            return response;
        }

        [Function(nameof(BlobInputBytesEnumerableTest))]
        public async Task<HttpResponseData> BlobInputBytesEnumerableTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated")] IEnumerable<byte[]> blobContent)
        {
            List<string> blobList = new();

            foreach (byte[] bytes in blobContent)
            {
                blobList.Add(Encoding.Default.GetString(bytes));
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            string contentAsString = string.Join(", ", blobList.ToArray());
            await response.WriteStringAsync(contentAsString);
            return response;
        }

        [Function(nameof(BlobInputBytesEnumerableSingleBlobTest))]
        public async Task<HttpResponseData> BlobInputBytesEnumerableSingleBlobTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated/testFile.txt")] IEnumerable<byte[]> blobContent)
        {
            List<string> blobList = new();

            foreach (byte[] bytes in blobContent)
            {
                blobList.Add(Encoding.Default.GetString(bytes));
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            string contentAsString = string.Join(", ", blobList.ToArray());
            await response.WriteStringAsync(contentAsString);
            return response;
        }

        [Function(nameof(BlobInputStringEnumerableTest))]
        public async Task<HttpResponseData> BlobInputStringEnumerableTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated")] IEnumerable<string> blobContent)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            string contentAsString = string.Join(", ", blobContent);
            await response.WriteStringAsync(contentAsString);
            return response;
        }

        [Function(nameof(BlobInputStringEnumerableSingleBlobTest))]
        public async Task<HttpResponseData> BlobInputStringEnumerableSingleBlobTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated/testFile.txt")] IEnumerable<string> blobContent)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            string contentAsString = string.Join(", ", blobContent);
            await response.WriteStringAsync(contentAsString);
            return response;
        }

        [Function(nameof(BlobInputPocoEnumerableTest))]
        public async Task<HttpResponseData> BlobInputPocoEnumerableTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated")] IEnumerable<Book> books)
        {
            List<string> bookNames = new();

            foreach (var item in books)
            {
                bookNames.Add(item.Name);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            string contentAsString = string.Join(", ", bookNames.ToArray());
            await response.WriteStringAsync(contentAsString);
            return response;
        }

        [Function(nameof(BlobInputPocoEnumerableSingleBlobTest))]
        public async Task<HttpResponseData> BlobInputPocoEnumerableSingleBlobTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated/testFile.txt")] IEnumerable<Book> books)
        {
            List<string> bookNames = new();

            foreach (var item in books)
            {
                bookNames.Add(item.Name);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            string contentAsString = string.Join(", ", bookNames.ToArray());
            await response.WriteStringAsync(contentAsString);
            return response;
        }

        [Function(nameof(BlobInputClientCollectionWithSubdirectoryTest))]
        public async Task<HttpResponseData> BlobInputClientCollectionWithSubdirectoryTest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("test-input-dotnet-isolated/test")] IEnumerable<BlobClient> blobs)
        {
            List<string> blobList = new();

            foreach (BlobClient blob in blobs)
            {
                var downloadResult = await blob.DownloadContentAsync();
                var content = downloadResult.Value.Content.ToString();
                blobList.Add(content);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            string contentAsString = string.Join(", ", blobList.ToArray());
            await response.WriteStringAsync(contentAsString);
            return response;
        }
    }
}