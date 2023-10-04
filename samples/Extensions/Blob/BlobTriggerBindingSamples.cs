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
    /// <summary>
    /// Samples demonstrating binding to the types supported by the `BlobTrigger` binding.
    /// </summary>
    public class BlobTriggerBindingSamples
    {
        private readonly ILogger<BlobTriggerBindingSamples> _logger;

        public BlobTriggerBindingSamples(ILogger<BlobTriggerBindingSamples> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// This sample demonstrates how to retrieve the contents of a blob file when a blob
        /// is added or updated in the given container. The code uses a <see cref="BlobClient"/>
        /// instance to read contents of the blob. The string {name} in the blob trigger path
        /// creates a binding expression that you can use in function code to access the file
        /// name of the triggering blob.
        /// The <see cref="BlobClient"/> instance could also be used for write operations.
        /// </summary>
        [Function(nameof(BlobClientFunction))]
        public async Task BlobClientFunction(
            [BlobTrigger("client-trigger/{name}")] BlobClient client, string name)
        {
            var downloadResult = await client.DownloadContentAsync();
            var content = downloadResult.Value.Content.ToString();
            _logger.LogInformation("Blob name: {name} -- Blob content: {content}", name, content);
        }

        /// <summary>
        /// This sample demonstrates how to retrieve the contents of a blob file when a blob
        /// is added or updated in the given container by binding to a <see cref="Stream"/>.
        /// The string {name} in the blob trigger path creates a binding expression that you
        /// can use in function code to access the file name of the triggering blob.
        /// </summary>
        [Function(nameof(BlobStreamFunction))]
        public async Task BlobStreamFunction(
            [BlobTrigger("stream-trigger/{name}")] Stream stream, string name)
        {
            using var blobStreamReader = new StreamReader(stream);
            var content = await blobStreamReader.ReadToEndAsync();
            _logger.LogInformation("Blob name: {name} -- Blob content: {content}", name, content);
        }

        /// <summary>
        /// This sample demonstrates how to retrieve the contents of a blob file when a blob
        /// is added or updated in the given container by binding to a <see cref="Byte[]"/>.
        /// </summary>
        [Function(nameof(BlobByteArrayFunction))]
        public void BlobByteArrayFunction(
            [BlobTrigger("byte-trigger")] Byte[] data)
        {
            _logger.LogInformation("Blob content: {content}", Encoding.Default.GetString(data));
        }

        /// <summary>
        /// This sample demonstrates how to retrieve the contents of a blob file when a blob
        /// is added or updated in the given container by binding to a <see cref="string"/>.
        /// </summary>
        [Function(nameof(BlobStringFunction))]
        public void BlobStringFunction(
            [BlobTrigger("string-trigger")] string data)
        {
            _logger.LogInformation("Blob content: {content}", data);
        }

        /// <summary>
        /// This sample demonstrates how to retrieve the contents of a blob file when a blob
        /// is added or updated in the given container by binding to a <see cref="Book"/> (POCO).
        /// The content of the blob must be JSON deserializable into the type of the parameter.
        /// </summary>
        [Function(nameof(BlobBookFunction))]
        public void BlobBookFunction(
            [BlobTrigger("book-trigger")] Book data)
        {
            _logger.LogInformation("Id: {id} - Name: {name}", data.Id, data.Name);
        }
    }
}
