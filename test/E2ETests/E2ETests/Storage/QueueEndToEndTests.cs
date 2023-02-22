// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.Functions.Tests.E2ETests.Storage
{
    [Collection(Constants.FunctionAppCollectionName)]
    public class QueueEndToEndTests
    {
        private FunctionAppFixture _fixture;

        public QueueEndToEndTests(FunctionAppFixture fixture)
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
            //Clear queue
            await StorageHelpers.ClearQueue(Constants.Queue.InputListBindingName);
            await StorageHelpers.ClearQueue(Constants.Queue.OutputListBindingName);

            //Set up and trigger
            await StorageHelpers.CreateQueue(Constants.Queue.OutputListBindingName);
            await StorageHelpers.InsertIntoQueue(Constants.Queue.InputListBindingName, expectedQueueMessage);

            //Verify
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
            //Clear queue
            await StorageHelpers.ClearQueue(Constants.Queue.InputBindingDataName);
            await StorageHelpers.ClearQueue(Constants.Queue.OutputBindingDataName);

            //Set up and trigger
            await StorageHelpers.CreateQueue(Constants.Queue.OutputBindingDataName);
            await StorageHelpers.InsertIntoQueue(Constants.Queue.InputBindingDataName, expectedQueueMessage);

            //Verify
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
    }
}
