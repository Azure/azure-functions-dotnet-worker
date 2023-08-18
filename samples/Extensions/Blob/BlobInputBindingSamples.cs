// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    /// <summary>
    /// Samples demonstrating binding to the types supported by the `BlobInput` binding.
    /// </summary>
    public class BlobInputBindingSamples
    {
        private readonly ILogger<BlobInputBindingSamples> _logger;

        public BlobInputBindingSamples(ILogger<BlobInputBindingSamples> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// This sample demonstrates how to retrieve the blobs within a container
        /// The code uses a <see cref="BlobContainerClient"/> instance to read get
        /// an async sequence of blobs in the given container.
        /// </summary>
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

        /// <summary>
        /// This sample demonstrates how to retrieve the contents of a given blob file.
        /// The code uses a <see cref="BlobClient"/> instance to read contents of the blob.
        /// The <see cref="BlobClient"/> instance could also be used for write operations.
        /// </summary>
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

        /// <summary>
        /// This sample demonstrates how to retrieve the contents all the blobs within a container.
        /// The code uses a <see cref="IEnumerable<T>"/> of type <see cref="BlobClient"/> to retrieve
        /// a <see cref="BlobClient"/> list of all the blobs within a given container, and then reads
        /// the contents of each blob.
        /// </summary>
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

        /// <summary>
        /// This sample demonstrates how to retrieve the contents of a given blob file
        /// by binding to a <see cref="Stream"/>. This function also demonstrates how to
        /// bind to a parameter from the route to get the blob name.
        /// Example usage: api/BlobInputStreamFunction?filename=sample1.txt
        /// </summary>
        [Function(nameof(BlobInputStreamFunction))]
        public async Task<HttpResponseData> BlobInputStreamFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("input-container/{filename}")] Stream stream)
        {
            using var blobStreamReader = new StreamReader(stream);
            _logger.LogInformation("Blob content: {content}", await blobStreamReader.ReadToEndAsync());
            return req.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// This sample demonstrates how to retrieve the content of all the blobs within a folder in a
        /// given container. The code uses a <see cref="Array"/> of type <see cref="string"/>
        /// to retrieve an array containing all the <see cref="string"/> content of the blobs in the file path.
        /// </summary>
        [Function(nameof(BlobInputCollectionSubdirectoryFunction))]
        public HttpResponseData BlobInputCollectionSubdirectoryFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("input-container/test")] string[] testFolderBlobs)
        {
            _logger.LogInformation("Content of all blobs within the 'test' subdirectory in the container:");

            foreach (var blobContents in testFolderBlobs)
            {
                _logger.LogInformation(blobContents);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// This sample demonstrates how to retrieve the contents of a given blob file as a collection.
        /// The code uses a <see cref="Array"/> of type <see cref="Book"/> to retrieve an array containing
        /// the contents of the given blob file, which is a JSON array of books.
        /// The content of the blob must be JSON deserializable into the type of the parameter.
        /// </summary>
        [Function(nameof(BlobInputBookArrayFileContentFunction))]
        public HttpResponseData BlobInputBookArrayFileContentFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [BlobInput("input-container/manybooks.json")] Book[] blobContent)
        {
            _logger.LogInformation("Content of single file as array:");

            foreach (var item in blobContent)
            {
                _logger.LogInformation(item.Name);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
