// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SampleApp
{
    public class QueueTriggerBindingSamples
    {
        private readonly ILogger<QueueTriggerBindingSamples> _logger;

        public QueueTriggerBindingSamples(ILogger<QueueTriggerBindingSamples> logger)
        {
            _logger = logger;
        }

        [Function(nameof(QueueMessageFunction))]
        public void QueueMessageFunction([QueueTrigger("input-queue")] QueueMessage message)
        {
            _logger.LogInformation(message.MessageText);
        }

        [Function(nameof(QueueBinaryDataFunction))]
        public void QueueBinaryDataFunction([QueueTrigger("input-queue-binarydata")] BinaryData message)
        {
            _logger.LogInformation(message.ToString());
        }

        [Function(nameof(QueueJsonFunction))]
        public void QueueJsonFunction([QueueTrigger("input-queue-json")] JsonElement message)
        {
            _logger.LogInformation(message.ToString());
        }
    }
}