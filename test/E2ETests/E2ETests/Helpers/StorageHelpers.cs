// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace Microsoft.Azure.Functions.Tests.E2ETests
{
    internal static class StorageHelpers
    {
        private const int SECONDS = 1000;
        private static QueueClient CreateQueueClient(string queueName)
        {
            var options = new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            };

            return new QueueClient(Constants.StorageConnectionStringSetting, queueName, options);
        }

        private static BlobContainerClient CreateBlobContainerClient(string containerName)
        {
            var options = new BlobClientOptions
            {
            };

            return new BlobContainerClient(Constants.StorageConnectionStringSetting, containerName, options);
        }

        public static async Task DeleteQueue(string queueName)
        {
            var queueClient = CreateQueueClient(queueName);
            await queueClient.DeleteIfExistsAsync();
        }

        public static async Task DeleteQueues()
        {
            await DeleteQueue(Constants.Queue.OutputBindingName);
            await DeleteQueue(Constants.Queue.OutputArrayBindingName);
            await DeleteQueue(Constants.Queue.OutputListBindingName);
            await DeleteQueue(Constants.Queue.OutputBindingDataName);
            await DeleteQueue(Constants.Queue.OutputBindingNameMetadata);
            await DeleteQueue(Constants.Queue.OutputBindingNamePOCO);

        }

        public static async Task ClearQueue(string queueName)
        {
            var queueClient = CreateQueueClient(queueName);
            if (await queueClient.ExistsAsync())
            {
                await queueClient.ClearMessagesAsync();
            }
        }

        public static async Task CreateQueue(string queueName)
        {
            var queueClient = CreateQueueClient(queueName);
            await queueClient.CreateIfNotExistsAsync();
        }

        public static async Task CreateQueues()
        {
            await StorageHelpers.CreateQueue(Constants.Queue.OutputBindingName);
            await StorageHelpers.CreateQueue(Constants.Queue.OutputArrayBindingName);
            await StorageHelpers.CreateQueue(Constants.Queue.OutputListBindingName);
            await StorageHelpers.CreateQueue(Constants.Queue.OutputBindingDataName);
            await StorageHelpers.CreateQueue(Constants.Queue.OutputBindingNameMetadata);
            await StorageHelpers.CreateQueue(Constants.Queue.OutputBindingNamePOCO);

        }

        public static async Task<string> InsertIntoQueue(string queueName, string queueMessage)
        {
            var messageBytes = Encoding.UTF8.GetBytes(queueMessage);
            string base64 = Convert.ToBase64String(messageBytes);

            var queueClient = CreateQueueClient(queueName);
            await queueClient.CreateIfNotExistsAsync();
            Response<SendReceipt> response = await queueClient.SendMessageAsync(queueMessage);
            return response.Value.MessageId;
        }

        public async static Task<string> ReadFromQueue(string queueName)
        {
            var queueClient = CreateQueueClient(queueName);
            QueueMessage retrievedMessage = null;
            await TestUtility.RetryAsync(async () =>
            {
                Response<QueueMessage> response = await queueClient.ReceiveMessageAsync();
                retrievedMessage = response.Value;
                return retrievedMessage != null;
            }, userMessageCallback: () => $"Failed to retrieve message from {queueName}");
            await queueClient.DeleteMessageAsync(retrievedMessage.MessageId, retrievedMessage.PopReceipt);
            return retrievedMessage.Body.ToString();
        }

        public async static Task<IEnumerable<string>> ReadMessagesFromQueue(string queueName)
        {
            var queueClient = CreateQueueClient(queueName);
            QueueMessage[] retrievedMessages = null;
            List<string> messages = new List<string>();
            await TestUtility.RetryAsync(async () =>
            {
                retrievedMessages = await queueClient.ReceiveMessagesAsync(maxMessages: 3);
                return retrievedMessages != null;
            });
            foreach (QueueMessage msg in retrievedMessages)
            {
                messages.Add(msg.Body.ToString());
                await queueClient.DeleteMessageAsync(msg.MessageId, msg.PopReceipt);
            }
            return messages;
        }

        public static async Task ClearBlobContainers()
        {
            await ClearBlobContainer(Constants.Blob.TriggerInputBindingContainer);
            await ClearBlobContainer(Constants.Blob.InputBindingContainer);
            await ClearBlobContainer(Constants.Blob.OutputBindingContainer);
            await ClearBlobContainer(Constants.Blob.TriggerPocoContainer);
            await ClearBlobContainer(Constants.Blob.OutputPocoContainer);
            await ClearBlobContainer(Constants.Blob.TriggerStringContainer);
            await ClearBlobContainer(Constants.Blob.OutputStringContainer);
        }

        public static async Task CreateBlobContainers()
        {
            await CreateBlobContainer(Constants.Blob.TriggerInputBindingContainer);
            await CreateBlobContainer(Constants.Blob.InputBindingContainer);
            await CreateBlobContainer(Constants.Blob.OutputBindingContainer);
            await CreateBlobContainer(Constants.Blob.TriggerPocoContainer);
            await CreateBlobContainer(Constants.Blob.OutputPocoContainer);
            await CreateBlobContainer(Constants.Blob.TriggerStringContainer);
            await CreateBlobContainer(Constants.Blob.OutputStringContainer);

        }

        public static async Task ClearQueues()
        {
            await ClearQueue(Constants.Queue.OutputBindingName);
            await ClearQueue(Constants.Queue.OutputArrayBindingName);
            await ClearQueue(Constants.Queue.OutputListBindingName);
            await ClearQueue(Constants.Queue.OutputBindingDataName);
            await ClearQueue(Constants.Queue.OutputBindingNameMetadata);
            await ClearQueue(Constants.Queue.OutputBindingNamePOCO);
        }

        public static Task UploadFileToContainer(string containerName, string fileName)
        {
            return UploadFileToContainer(containerName, fileName, "Hello World");
        }

        public async static Task UploadFileToContainer(string containerName, string fileName, string fileContents)
        {
            string sourceFile = $"{fileName}.txt";
            File.WriteAllText(sourceFile, fileContents);
            await CreateBlobContainer(containerName);
            BlobContainerClient cloudBlobContainer = CreateBlobContainerClient(containerName);
            BlobClient cloudBlockBlob = cloudBlobContainer.GetBlobClient(sourceFile);
            await cloudBlockBlob.UploadAsync(sourceFile);
        }

        public async static Task<string> DownloadFileFromContainer(string containerName, string expectedFileName)
        {
            string destinationFile = $"{expectedFileName}_DOWNLOADED.txt";
            string sourceFile = $"{expectedFileName}.txt";
            BlobContainerClient cloudBlobContainer = CreateBlobContainerClient(containerName);
            BlobClient cloudBlockBlob = cloudBlobContainer.GetBlobClient(sourceFile);
            await TestUtility.RetryAsync(async () => await cloudBlockBlob.ExistsAsync(), 
                pollingInterval: 2 * SECONDS , timeout: 120 * SECONDS);

            await cloudBlockBlob.DownloadToAsync(destinationFile);

            return File.ReadAllText(destinationFile);
        }


        private static async Task<BlobContainerClient> CreateBlobContainer(string containerName)
        {
            var cloudBlobContainer = CreateBlobContainerClient(containerName);
            await cloudBlobContainer.CreateIfNotExistsAsync();
            await cloudBlobContainer.SetAccessPolicyAsync(PublicAccessType.Blob);
            return cloudBlobContainer;
        }

        private static async Task DeleteBlobContainer(string containerName)
        {
            var client = new BlobServiceClient(Constants.StorageConnectionStringSetting);
            await client.DeleteBlobContainerAsync(containerName);
        }

        public static async Task DeleteBlobContainers()
        {
            await StorageHelpers.DeleteBlobContainer(Constants.Blob.TriggerInputBindingContainer);
            await StorageHelpers.DeleteBlobContainer(Constants.Blob.InputBindingContainer);
            await StorageHelpers.DeleteBlobContainer(Constants.Blob.OutputBindingContainer);

            await StorageHelpers.DeleteBlobContainer(Constants.Blob.TriggerPocoContainer);
            await StorageHelpers.DeleteBlobContainer(Constants.Blob.OutputPocoContainer);

            await StorageHelpers.DeleteBlobContainer(Constants.Blob.TriggerStringContainer);
            await StorageHelpers.DeleteBlobContainer(Constants.Blob.OutputStringContainer);
        }

        private static async Task ClearBlobContainer(string containerName)
        {
            BlobContainerClient cloudBlobContainer = CreateBlobContainerClient(containerName);

            if (!await cloudBlobContainer.ExistsAsync())
            {
                return;
            }

            await foreach (BlobItem item in cloudBlobContainer.GetBlobsAsync())
            {
                Console.WriteLine(item.Name);

                var cloudBlob = cloudBlobContainer.GetBlobClient(item.Name);
                await cloudBlob.DeleteIfExistsAsync();
            }
        }
    }
}
