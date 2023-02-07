﻿// Copyright (c) .NET Foundation. All rights reserved.
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
    public class BlobTriggerBindingSamples
    {
        private readonly ILogger<BlobTriggerBindingSamples> _logger;

        public BlobTriggerBindingSamples(ILogger<BlobTriggerBindingSamples> logger)
        {
            _logger = logger;
        }

        [Function(nameof(BlobClientFunction))]
        public async Task BlobClientFunction(
            [BlobTrigger("client-trigger/{name}")] BlobClient client)
        {
            var downloadResult = await client.DownloadContentAsync();
            var content = downloadResult.Value.Content.ToString();
            _logger.LogInformation("Blob name: {blobName}, content: {content}", client.Name, content);
        }

        [Function(nameof(BlobStreamFunction))]
        public async Task BlobStreamFunction(
            [BlobTrigger("stream-trigger/{name}")] Stream stream)
        {
            using var blobStreamReader = new StreamReader(stream);
            var content = await blobStreamReader.ReadToEndAsync();
            _logger.LogInformation("Blob content: {stream}", content);
        }

        [Function(nameof(BlobByteArrayFunction))]
        public void BlobByteArrayFunction(
            [BlobTrigger("byte-trigger/{name}")] Byte[] data)
        {
            _logger.LogInformation($"Blob content: {Encoding.Default.GetString(data)}");
        }

        [Function(nameof(BlobStringFunction))]
        public void BlobStringFunction(
            [BlobTrigger("string-trigger/{name}")] string data)
        {
            _logger.LogInformation($"Blob content: {data}");
        }

        [Function(nameof(BlobBookFunction))]
        public void BlobBookFunction(
            [BlobTrigger("book-trigger/{name}")] Book data)
        {
            _logger.LogInformation($"Id: {data.Id} - Name: {data.Name}");
        }
    }
}
