// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public class QueueFunction
    {
        private readonly ILogger<QueueFunction> _logger;

        public QueueFunction(ILogger<QueueFunction> logger)
        {
            _logger = logger;
        }

        //<docsnippet_queue_output_binding>
        //<docsnippet_queue_trigger>
        [Function(nameof(QueueFunction))]
        [QueueOutput("output-queue")]
        public string[] Run([QueueTrigger("input-queue")] Album myQueueItem, FunctionContext context)
        //</docsnippet_queue_trigger>
        {
            // Use a string array to return more than one message.
            string[] messages = {
                $"Album name = {myQueueItem.Name}",
                $"Album songs = {myQueueItem.Songs.ToString()}"};

            _logger.LogInformation("{msg1},{msg2}", messages[0], messages[1]);

            // Queue Output messages
            return messages;
        }
        //</docsnippet_queue_output_binding>

        /// <summary>
        /// This function demonstrates binding to a single <see cref="QueueMessage"/>.
        /// </summary>
        [Function(nameof(QueueMessageFunction))]
        public void QueueMessageFunction([QueueTrigger("input-queue")] QueueMessage message)
        {
            _logger.LogInformation(message.MessageText);
        }

        /// <summary>
        /// This function demonstrates binding to a single <see cref="BinaryData"/>.
        /// </summary>
        [Function(nameof(QueueBinaryDataFunction))]
        public void QueueBinaryDataFunction([QueueTrigger("input-queue")] BinaryData message)
        {
            _logger.LogInformation(message.ToString());
        }
    }

    public class Album
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public List<string> Songs { get; set; }
    }
}
