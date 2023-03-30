// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

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
            _logger.LogInformation(message.MessageText);
        }

        [Function(nameof(BinaryDataSample))]
        public void BinaryDataSample([QueueTrigger("input-queue-bd")] BinaryData message)
        {
            _logger.LogInformation(message.ToString());
        }

        [Function(nameof(JObjectSample))]
        public void JObjectSample([QueueTrigger("input-queue-jo")] JObject message)
        {
            _logger.LogInformation(message.ToString());
        }
    }
}