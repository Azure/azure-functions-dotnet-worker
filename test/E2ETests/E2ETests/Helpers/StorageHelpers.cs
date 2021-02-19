// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace Microsoft.Azure.Functions.Tests.E2ETests
{
    internal class StorageHelpers
    {
        private static QueueClient CreateDefaultClient(string queueName)
        {
            var options = new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            };

            return new QueueClient(Constants.Queue.StorageConnectionStringSetting, queueName, options);
        }

        public async static Task DeleteQueue(string queueName)
        {
            var queueClient = new QueueClient(Constants.Queue.StorageConnectionStringSetting, queueName);
            await queueClient.DeleteIfExistsAsync();
        }

        public async static Task ClearQueue(string queueName)
        {
            var queueClient = CreateDefaultClient(queueName);
            if (await queueClient.ExistsAsync())
            {
                await queueClient.ClearMessagesAsync();
            }
        }

        public async static Task CreateQueue(string queueName)
        {
            var queueClient = CreateDefaultClient(queueName);
            await queueClient.CreateIfNotExistsAsync();
        }

        public async static Task<string> InsertIntoQueue(string queueName, string queueMessage)
        {
            var messageBytes = Encoding.UTF8.GetBytes(queueMessage);
            string base64 = Convert.ToBase64String(messageBytes);

            var queueClient = CreateDefaultClient(queueName);
            await queueClient.CreateIfNotExistsAsync();
            Response<SendReceipt> response = await queueClient.SendMessageAsync(queueMessage);
            return response.Value.MessageId;
        }

        public async static Task<string> ReadFromQueue(string queueName)
        {
            var queueClient = CreateDefaultClient(queueName);
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
            var queueClient = CreateDefaultClient(queueName);
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
    }
}
