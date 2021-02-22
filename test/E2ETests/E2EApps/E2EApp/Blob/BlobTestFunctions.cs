// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Storage;
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

        [FunctionName("BlobTriggerToBlobTest")]
        [BlobOutput("outputBlob", "test-output-dotnet-isolated/{name}")]
        public void BlobTriggerToBlobTest(
            [BlobTrigger("test-triggerinput-dotnet-isolated/{name}")] byte[] triggerBlob, string name,
            [BlobInput("test-input-dotnet-isolated/{name}")] byte[] inputBlob,
            FunctionContext context)
        {
            _logger.LogInformation("Trigger:\n Name: " + name + "\n Size: " + triggerBlob.Length + " Bytes");
            _logger.LogInformation("Input:\n Name: " + name + "\n Size: " + inputBlob.Length + " Bytes");
            context.OutputBindings["outputBlob"] = inputBlob;
        }

        [FunctionName("BlobTriggerPOCOTest")]
        [BlobOutput("outputBlob", "test-outputpoco-dotnet-isolated/{name}")]
        public void BlobTriggerPocoTest(
            [BlobTrigger("test-triggerinputpoco-dotnet-isolated/{name}")] TestBlobData triggerBlob, string name,
            FunctionContext context)
        {
            _logger.LogInformation(".NET Blob trigger function processed a blob.\n Name: " + name + "\n Content: " + triggerBlob.BlobText);
            context.OutputBindings["outputBlob"] = triggerBlob;
        }

        [FunctionName("BlobTriggerStringTest")]
        [BlobOutput("outputBlob", "test-outputstring-dotnet-isolated/{name}")]
        public void BlobTriggerStringTest(
            [BlobTrigger("test-triggerinputstring-dotnet-isolated/{name}")] string triggerBlobText, string name,
            FunctionContext context)
        {
            _logger.LogInformation(".NET Blob trigger function processed a blob.\n Name: " + name + "\n Content: " + triggerBlobText);
            context.OutputBindings["outputBlob"] = triggerBlobText;
        }

        public class TestBlobData
        {
            [JsonPropertyName("text")]
            public string BlobText { get; set; }
        }
    }
}
