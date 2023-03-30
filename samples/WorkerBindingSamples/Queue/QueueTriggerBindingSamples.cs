// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public class QueueTriggerBindingSamples
    {
        private readonly ILogger<QueueTriggerBindingSamples> _logger;

        public QueueTriggerBindingSamples(ILogger<QueueTriggerBindingSamples> logger)
        {
            _logger = logger;
        }

        [Function(nameof(QueueMessageSample))]
        public void QueueMessageSample([QueueTrigger("input-queue")] QueueMessage message)
        {
            _logger.LogInformation(message.Body.ToString());
        }
    }
}