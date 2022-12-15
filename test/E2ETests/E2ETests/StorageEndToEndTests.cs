// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.Functions.Tests.E2ETests
{
    [Collection(Constants.StorageFunctionAppCollectionName)]
    public class StorageEndToEndTests
    {
        private const int SECONDS = 1000;
        
        private readonly FunctionAppFixture _fixture;

        public StorageEndToEndTests(StorageFunctionAppFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task QueueTriggerAndOutput_Succeeds()
        {
            string expectedQueueMessage = Guid.NewGuid().ToString();
            //Clear queue
            await StorageHelpers.ClearQueue(Constants.Queue.OutputBindingName);
            await StorageHelpers.ClearQueue(Constants.Queue.InputBindingName);

            //Set up and trigger
            await StorageHelpers.CreateQueue(Constants.Queue.OutputBindingName);
            await StorageHelpers.InsertIntoQueue(Constants.Queue.InputBindingName, expectedQueueMessage);

            //Verify
            await CheckLogForExecutionOf("Functions.QueueTriggerAndOutput");

            var queueMessage = await StorageHelpers.ReadFromQueue(Constants.Queue.OutputBindingName);
            Assert.Equal(expectedQueueMessage, queueMessage);
        }

        [Fact]
        public async Task QueueTriggerAndArrayOutput_Succeeds()
        {
            string expectedQueueMessage = Guid.NewGuid().ToString();

            //Set up and trigger
            await StorageHelpers.InsertIntoQueue(Constants.Queue.InputArrayBindingName, expectedQueueMessage);

            //Verify
            await CheckLogForExecutionOf("Functions.QueueTriggerAndArrayOutput");

            string queueMessage1 = await StorageHelpers.ReadFromQueue(Constants.Queue.OutputArrayBindingName);
            string[] splitMessage1 = queueMessage1.Split("|");
            Assert.Equal(expectedQueueMessage, splitMessage1[0]);
            Assert.True(string.Equals("1", splitMessage1[1]) || string.Equals("2", splitMessage1[1]));

            string queueMessage2 = await StorageHelpers.ReadFromQueue(Constants.Queue.OutputArrayBindingName);
            string[] splitMessage2 = queueMessage2.Split("|");
            Assert.Equal(expectedQueueMessage, splitMessage2[0]);
            Assert.True(string.Equals("1", splitMessage2[1]) || string.Equals("2", splitMessage2[1]));

            Assert.NotEqual(queueMessage1, queueMessage2);
        }

        [Fact]
        public async Task QueueTriggerAndListOutput_Succeeds()
        {
            string expectedQueueMessage = Guid.NewGuid().ToString();

            //Set up and trigger
            await StorageHelpers.InsertIntoQueue(Constants.Queue.InputListBindingName, expectedQueueMessage);

            //Verify
            await CheckLogForExecutionOf("Functions.QueueTriggerAndListOutput");

            string queueMessage1 = await StorageHelpers.ReadFromQueue(Constants.Queue.OutputListBindingName);
            string[] splitMessage1 = queueMessage1.Split("|");
            Assert.Equal(expectedQueueMessage, splitMessage1[0]);
            Assert.True(string.Equals("1", splitMessage1[1]) || string.Equals("2", splitMessage1[1]));

            string queueMessage2 = await StorageHelpers.ReadFromQueue(Constants.Queue.OutputListBindingName);
            string[] splitMessage2 = queueMessage2.Split("|");
            Assert.Equal(expectedQueueMessage, splitMessage2[0]);
            Assert.True(string.Equals("1", splitMessage2[1]) || string.Equals("2", splitMessage2[1]));

            Assert.NotEqual(queueMessage1, queueMessage2);
        }

        [Fact]
        public async Task QueueTriggerAndBindingDataOutput_Succeeds()
        {
            string expectedQueueMessage = Guid.NewGuid().ToString();

            //Set up and trigger
            await StorageHelpers.InsertIntoQueue(Constants.Queue.InputBindingDataName, expectedQueueMessage);

            //Verify
            await CheckLogForExecutionOf("Functions.QueueTriggerAndBindingDataOutput");

            string resultMessage = await StorageHelpers.ReadFromQueue(Constants.Queue.OutputBindingDataName);
            IDictionary<string, string> splitMessage = resultMessage.Split(",").ToDictionary(s => s.Split('=')[0], s => s.Split('=')[1]);

            Assert.Contains("QueueTrigger", splitMessage);
            Assert.Contains("DequeueCount", splitMessage);
            Assert.Contains("Id", splitMessage);
            Assert.Contains("InsertionTime", splitMessage);
            Assert.Contains("NextVisibleTime", splitMessage);
            Assert.Contains("PopReceipt", splitMessage);
        }

        [Fact]
        public async Task QueueTrigger_BindToTriggerMetadata_Succeeds()
        {
            string inputQueueMessage = Guid.NewGuid().ToString();
            //Clear queue

            //Set up and trigger
            await StorageHelpers.CreateQueue(Constants.Queue.OutputBindingNameMetadata);
            string expectedQueueMessage = await StorageHelpers.InsertIntoQueue(Constants.Queue.InputBindingNameMetadata, inputQueueMessage);

            //Verify
            await CheckLogForExecutionOf("Functions.QueueTriggerMetadata");

            var queueMessage = await StorageHelpers.ReadFromQueue(Constants.Queue.OutputBindingNameMetadata);
            Assert.Contains(expectedQueueMessage, queueMessage);
        }

        [Fact]
        public async Task QueueTrigger_QueueOutput_Poco_Succeeds()
        {
            string expectedQueueMessage = Guid.NewGuid().ToString();

            //Set up and trigger
            await StorageHelpers.CreateQueue(Constants.Queue.OutputBindingNamePOCO);

            string json = JsonSerializer.Serialize(new { id = expectedQueueMessage });

            await StorageHelpers.InsertIntoQueue(Constants.Queue.InputBindingNamePOCO, json);

            //Verify
            await CheckLogForExecutionOf("Functions.QueueTriggerAndOutputPoco");

            var queueMessage = await StorageHelpers.ReadFromQueue(Constants.Queue.OutputBindingNamePOCO);
            Assert.Contains(expectedQueueMessage, queueMessage);
        }

        [Fact]
        public async Task QueueOutput_PocoList_Succeeds()
        {
            string expectedQueueMessage = Guid.NewGuid().ToString();

            //Trigger
            Assert.True(await HttpHelpers.InvokeHttpTrigger("QueueOutputPocoList", $"?queueMessageId={expectedQueueMessage}", HttpStatusCode.OK, expectedQueueMessage));

            //Verify
            IEnumerable<string> queueMessages = await StorageHelpers.ReadMessagesFromQueue(Constants.Queue.OutputBindingNamePOCO);
            Assert.True(queueMessages.All(msg => msg.Contains(expectedQueueMessage)));
        }

        [Fact]
        public async Task BlobTriggerToBlob_Succeeds()
        {
            string fileName = Guid.NewGuid().ToString();

            //Setup
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, fileName);

            //Trigger
            await StorageHelpers.UploadFileToContainer(Constants.Blob.TriggerInputBindingContainer, fileName);

            //Verify
            await CheckLogForExecutionOf("Functions.BlobTriggerToBlobTest");

            string result = await StorageHelpers.DownloadFileFromContainer(Constants.Blob.OutputBindingContainer, fileName);

            Assert.Equal("Hello World", result);
        }

        [Fact]
        public async Task BlobTriggerPoco_Succeeds()
        {
            string fileName = Guid.NewGuid().ToString();

            //Trigger
            var json = JsonSerializer.Serialize(new { text = "Hello World" });
            await StorageHelpers.UploadFileToContainer(Constants.Blob.TriggerPocoContainer, fileName, json);

            //Verify
            await CheckLogForExecutionOf("Functions.BlobTriggerPocoTest");

            string result = await StorageHelpers.DownloadFileFromContainer(Constants.Blob.OutputPocoContainer, fileName);

            Assert.Equal(json, result);
        }

        [Fact]
        public async Task BlobTriggerString_Succeeds()
        {
            string fileName = Guid.NewGuid().ToString();

            //Trigger
            await StorageHelpers.UploadFileToContainer(Constants.Blob.TriggerStringContainer, fileName);

            //Verify
            await CheckLogForExecutionOf("Functions.BlobTriggerStringTest");

            string result = await StorageHelpers.DownloadFileFromContainer(Constants.Blob.OutputStringContainer, fileName);

            Assert.Equal("Hello World", result);
        }

        private async Task CheckLogForExecutionOf(string functionName)
        {
            await TestUtility.RetryAsync(() => { 
                var _ = _fixture.TestLogs.CoreToolsLogs.Any(x => x.Contains($"Executed '{functionName}'"));
                return Task.FromResult(_);
            }, 
            timeout: 15 * SECONDS,
            userMessageCallback: () => "Executed log was not found");
        }
    }
}
