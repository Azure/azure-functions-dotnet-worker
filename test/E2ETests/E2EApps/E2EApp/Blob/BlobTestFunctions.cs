// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp.Blob
{
    public class BlobTestFunctions
    {
        private readonly ILogger<BlobTestFunctions> _logger;

        public BlobTestFunctions(ILogger<BlobTestFunctions> logger)
        {
            _logger = logger;
        }

        [Function(nameof(BlobTriggerToBlobTest))]
        [BlobOutput("test-output-dotnet-isolated/{name}")]
        public byte[] BlobTriggerToBlobTest(
            [BlobTrigger("test-triggerinput-dotnet-isolated/{name}")] byte[] triggerBlob, string name,
            [BlobInput("test-input-dotnet-isolated/{name}")] byte[] inputBlob,
            FunctionContext context)
        {
            _logger.LogInformation("Trigger:\n Name: " + name + "\n Size: " + triggerBlob.Length + " Bytes");
            _logger.LogInformation("Input:\n Name: " + name + "\n Size: " + inputBlob.Length + " Bytes");
            return inputBlob;
        }

        [Function(nameof(BlobTriggerPocoTest))]
        [BlobOutput("test-outputpoco-dotnet-isolated/{name}")]
        public TestBlobData BlobTriggerPocoTest(
            [BlobTrigger("test-triggerinputpoco-dotnet-isolated/{name}")] TestBlobData triggerBlob, string name,
            FunctionContext context)
        {
            _logger.LogInformation(".NET Blob trigger function processed a blob.\n Name: " + name + "\n Content: " + triggerBlob.BlobText);
            return triggerBlob;
        }

        [Function(nameof(BlobTriggerStringTest))]
        [BlobOutput("test-outputstring-dotnet-isolated/{name}")]
        public string BlobTriggerStringTest(
            [BlobTrigger("test-triggerinputstring-dotnet-isolated/{name}")] string triggerBlobText, string name,
            FunctionContext context)
        {
            _logger.LogInformation(".NET Blob trigger function processed a blob.\n Name: " + name + "\n Content: " + triggerBlobText);
            return triggerBlobText;
        }

        public class TestBlobData
        {
            [JsonPropertyName("text")]
            public string BlobText { get; set; }
        }
    }
}
