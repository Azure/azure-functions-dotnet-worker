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
    [Collection(Constants.FunctionAppCollectionName)]
    public class StorageEndToEndTests
    {
        private FunctionAppFixture _fixture;

        public StorageEndToEndTests(FunctionAppFixture fixture)
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
            var queueMessage = await StorageHelpers.ReadFromQueue(Constants.Queue.OutputBindingName);
            Assert.Equal(expectedQueueMessage, queueMessage);
        }

        [Fact]
        public async Task QueueTriggerAndArrayOutput_Succeeds()
        {
            string expectedQueueMessage = Guid.NewGuid().ToString();
            //Clear queue
            await StorageHelpers.ClearQueue(Constants.Queue.InputArrayBindingName);
            await StorageHelpers.ClearQueue(Constants.Queue.OutputArrayBindingName);

            //Set up and trigger            
            await StorageHelpers.CreateQueue(Constants.Queue.OutputArrayBindingName);
            await StorageHelpers.InsertIntoQueue(Constants.Queue.InputArrayBindingName, expectedQueueMessage);

            //Verify
            var queueMessage1 = await StorageHelpers.ReadFromQueue(Constants.Queue.OutputArrayBindingName);
            Assert.True(string.Equals(expectedQueueMessage + "-1", queueMessage1) || string.Equals(expectedQueueMessage + "-2", queueMessage1));
            var queueMessage2 = await StorageHelpers.ReadFromQueue(Constants.Queue.OutputArrayBindingName);
            Assert.True(string.Equals(expectedQueueMessage + "-1", queueMessage2) || string.Equals(expectedQueueMessage + "-2", queueMessage2));
        }

        [Fact]
        public async Task QueueTriggerAndListOutput_Succeeds()
        {
            string expectedQueueMessage = Guid.NewGuid().ToString();
            //Clear queue
            await StorageHelpers.ClearQueue(Constants.Queue.InputListBindingName);
            await StorageHelpers.ClearQueue(Constants.Queue.OutputListBindingName);

            //Set up and trigger            
            await StorageHelpers.CreateQueue(Constants.Queue.OutputListBindingName);
            await StorageHelpers.InsertIntoQueue(Constants.Queue.InputListBindingName, expectedQueueMessage);

            //Verify
            var queueMessage1 = await StorageHelpers.ReadFromQueue(Constants.Queue.OutputListBindingName);
            Assert.True(string.Equals(expectedQueueMessage + "-1", queueMessage1) || string.Equals(expectedQueueMessage + "-2", queueMessage1));
            var queueMessage2 = await StorageHelpers.ReadFromQueue(Constants.Queue.OutputListBindingName);
            Assert.True(string.Equals(expectedQueueMessage + "-1", queueMessage2) || string.Equals(expectedQueueMessage + "-2", queueMessage2));
        }

        [Fact]
        public async Task QueueTrigger_BindToTriggerMetadata_Succeeds()
        {
            string inputQueueMessage = Guid.NewGuid().ToString();
            //Clear queue
            await StorageHelpers.ClearQueue(Constants.Queue.OutputBindingNameMetadata);
            await StorageHelpers.ClearQueue(Constants.Queue.InputBindingNameMetadata);

            //Set up and trigger            
            await StorageHelpers.CreateQueue(Constants.Queue.OutputBindingNameMetadata);
            string expectedQueueMessage = await StorageHelpers.InsertIntoQueue(Constants.Queue.InputBindingNameMetadata, inputQueueMessage);

            //Verify
            var queueMessage = await StorageHelpers.ReadFromQueue(Constants.Queue.OutputBindingNameMetadata);
            Assert.Contains(expectedQueueMessage, queueMessage);
        }

        [Fact]
        public async Task QueueTrigger_QueueOutput_Poco_Succeeds()
        {
            string expectedQueueMessage = Guid.NewGuid().ToString();
            //Clear queue
            await StorageHelpers.ClearQueue(Constants.Queue.OutputBindingNamePOCO);
            await StorageHelpers.ClearQueue(Constants.Queue.InputBindingNamePOCO);

            //Set up and trigger            
            await StorageHelpers.CreateQueue(Constants.Queue.OutputBindingNamePOCO);

            string json = JsonSerializer.Serialize(new { id = expectedQueueMessage });

            await StorageHelpers.InsertIntoQueue(Constants.Queue.InputBindingNamePOCO, json);

            //Verify
            var queueMessage = await StorageHelpers.ReadFromQueue(Constants.Queue.OutputBindingNamePOCO);
            Assert.Contains(expectedQueueMessage, queueMessage);
        }

        [Fact]
        public async Task QueueOutput_PocoList_Succeeds()
        {
            string expectedQueueMessage = Guid.NewGuid().ToString();

            //Clear queue
            await StorageHelpers.ClearQueue(Constants.Queue.OutputBindingNamePOCO);

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

            //cleanup
            await StorageHelpers.ClearBlobContainers();

            //Setup
            await StorageHelpers.UploadFileToContainer(Constants.Blob.InputBindingContainer, fileName);

            //Trigger
            await StorageHelpers.UploadFileToContainer(Constants.Blob.TriggerInputBindingContainer, fileName);

            //Verify
            string result = await StorageHelpers.DownloadFileFromContainer(Constants.Blob.OutputBindingContainer, fileName);

            Assert.Equal("Hello World", result);
        }

        [Fact]
        public async Task BlobTriggerPoco_Succeeds()
        {
            string fileName = Guid.NewGuid().ToString();

            //cleanup
            await StorageHelpers.ClearBlobContainers();

            //Trigger
            var json = JsonSerializer.Serialize(new { text = "Hello World" });
            await StorageHelpers.UploadFileToContainer(Constants.Blob.TriggerPocoContainer, fileName, json);

            //Verify
            string result = await StorageHelpers.DownloadFileFromContainer(Constants.Blob.OutputPocoContainer, fileName);

            Assert.Equal(json, result);
        }

        [Fact]
        public async Task BlobTriggerString_Succeeds()
        {
            string fileName = Guid.NewGuid().ToString();

            //cleanup
            await StorageHelpers.ClearBlobContainers();

            //Trigger            
            await StorageHelpers.UploadFileToContainer(Constants.Blob.TriggerStringContainer, fileName);

            //Verify
            string result = await StorageHelpers.DownloadFileFromContainer(Constants.Blob.OutputStringContainer, fileName);

            Assert.Equal("Hello World", result);
        }
    }
}
