// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp.Blob
{
    public class BlobTriggerBindingFunctions
    {
        private readonly ILogger<BlobTriggerBindingFunctions> _logger;

        public BlobTriggerBindingFunctions(ILogger<BlobTriggerBindingFunctions> logger)
        {
            _logger = logger;
        }

        [Function(nameof(BlobTriggerToBlobTest))]
        [BlobOutput("test-output-dotnet-isolated/{name}")]
        public byte[] BlobTriggerToBlobTest(
            [BlobTrigger("test-trigger-dotnet-isolated/{name}")] byte[] triggerBlob, string name,
            [BlobInput("test-input-dotnet-isolated/{name}")] byte[] inputBlob,
            FunctionContext context)
        {
            _logger.LogInformation("Trigger:\n Name: " + name + "\n Size: " + triggerBlob.Length + " Bytes");
            _logger.LogInformation("Input:\n Name: " + name + "\n Size: " + inputBlob.Length + " Bytes");
            return inputBlob;
        }

        [Function(nameof(BlobTriggerPocoTest))]
        [BlobOutput("test-output-poco-dotnet-isolated/{name}")]
        public TestBlobData BlobTriggerPocoTest(
            [BlobTrigger("test-trigger-poco-dotnet-isolated/{name}")] TestBlobData triggerBlob, string name,
            FunctionContext context)
        {
            _logger.LogInformation(".NET Blob trigger function processed a blob.\n Name: " + name + "\n Content: " + triggerBlob.BlobText);
            return triggerBlob;
        }

        [Function(nameof(BlobTriggerStringTest))]
        [BlobOutput("test-output-string-dotnet-isolated/{name}")]
        public string BlobTriggerStringTest(
            [BlobTrigger("test-trigger-string-dotnet-isolated/{name}")] string triggerBlobText, string name,
            FunctionContext context)
        {
            _logger.LogInformation(".NET Blob trigger function processed a blob.\n Name: " + name + "\n Content: " + triggerBlobText);
            return triggerBlobText;
        }

        [Function(nameof(BlobTriggerStreamTest))]
        public async Task BlobTriggerStreamTest(
            [BlobTrigger("test-trigger-stream-dotnet-isolated/{name}")] Stream stream, string name,
            FunctionContext context)
        {
            using var blobStreamReader = new StreamReader(stream);
            string content = await blobStreamReader.ReadToEndAsync();
            _logger.LogInformation("StreamTriggerOutput: {c}", content);
        }

        [Function(nameof(BlobTriggerBlobClientTest))]
        public async Task BlobTriggerBlobClientTest(
            [BlobTrigger("test-trigger-blobclient-dotnet-isolated/{name}")] BlobClient client, string name,
            FunctionContext context)
        {
            var downloadResult = await client.DownloadContentAsync();
            string content = downloadResult.Value.Content.ToString();
            _logger.LogInformation("BlobClientTriggerOutput: {c}", content);
        }

        [Function(nameof(BlobTriggerBlobContainerClientTest))]
        public async Task BlobTriggerBlobContainerClientTest(
            [BlobTrigger("test-trigger-containerclient-dotnet-isolated/{name}")] BlobContainerClient client, string name,
            FunctionContext context)
        {
            var blobClient = client.GetBlobClient(name);
            var downloadResult = await blobClient.DownloadContentAsync();
            string content = downloadResult.Value.Content.ToString();
            _logger.LogInformation("BlobContainerTriggerOutput: {c}", content);
        }

        public class TestBlobData
        {
            [JsonPropertyName("text")]
            public string BlobText { get; set; }
        }
    }
}
