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
using Microsoft.Azure.Functions.Tests;

namespace Microsoft.Azure.Functions.Worker.E2ETests
{
    internal static class StorageHelpers
    {
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

        public async static Task DeleteQueue(string queueName)
        {
            var queueClient = CreateQueueClient(queueName);
            await queueClient.DeleteIfExistsAsync();
        }

        public async static Task ClearQueue(string queueName)
        {
            var queueClient = CreateQueueClient(queueName);
            if (await queueClient.ExistsAsync())
            {
                await queueClient.ClearMessagesAsync();
            }
        }

        public async static Task CreateQueue(string queueName)
        {
            var queueClient = CreateQueueClient(queueName);
            await queueClient.CreateIfNotExistsAsync();
        }

        public async static Task<string> InsertIntoQueue(string queueName, string queueMessage)
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
            });
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

        public async static Task ClearBlobContainers()
        {
            await ClearBlobContainer(Constants.Blob.TriggerInputBindingContainer);
            await ClearBlobContainer(Constants.Blob.InputBindingContainer);
            await ClearBlobContainer(Constants.Blob.OutputBindingContainer);
            await ClearBlobContainer(Constants.Blob.TriggerPocoContainer);
            await ClearBlobContainer(Constants.Blob.OutputPocoContainer);
            await ClearBlobContainer(Constants.Blob.TriggerStringContainer);
            await ClearBlobContainer(Constants.Blob.OutputStringContainer);
            await ClearBlobContainer(Constants.Blob.TriggerStreamContainer);
            await ClearBlobContainer(Constants.Blob.OutputStreamContainer);
            await ClearBlobContainer(Constants.Blob.TriggerBlobClientContainer);
            await ClearBlobContainer(Constants.Blob.OutputBlobClientContainer);
            await ClearBlobContainer(Constants.Blob.TriggerBlobContainerClientContainer);
            await ClearBlobContainer(Constants.Blob.OutputBlobContainerClientContainer);
        }

        public static Task UploadFileToContainer(string containerName, string fileName)
        {
            return UploadFileToContainer(containerName, fileName, "Hello World");
        }

        public async static Task UploadFileToContainer(string containerName, string fileName, string fileContents, bool containsSubdirectory = false)
        {
            string sourceFile = $"{fileName}.txt";

            if (containsSubdirectory)
            {
                string directoryPath = Path.GetDirectoryName(sourceFile);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            }

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
            await TestUtility.RetryAsync(async () =>
            {
                return await cloudBlockBlob.ExistsAsync();
            }, pollingInterval: 4000, timeout: 120 * 1000);

            await cloudBlockBlob.DownloadToAsync(destinationFile);

            return File.ReadAllText(destinationFile);
        }


        private static async Task<BlobContainerClient> CreateBlobContainer(string containerName)
        {
            BlobContainerClient cloudBlobContainer = CreateBlobContainerClient(containerName);
            await cloudBlobContainer.CreateIfNotExistsAsync();
            await cloudBlobContainer.SetAccessPolicyAsync(PublicAccessType.Blob);
            return cloudBlobContainer;
        }

        private static async Task ClearBlobContainer(string containerName)
        {
            BlobContainerClient cloudBlobContainer = CreateBlobContainerClient(containerName);

            if (!cloudBlobContainer.Exists())
            {
                return;
            }

            await foreach (BlobItem item in cloudBlobContainer.GetBlobsAsync())
            {
                Console.WriteLine(item.Name);
                BlobClient cloudBlob = cloudBlobContainer.GetBlobClient(item.Name);
                await cloudBlob.DeleteIfExistsAsync();
            }
        }
    }
}
