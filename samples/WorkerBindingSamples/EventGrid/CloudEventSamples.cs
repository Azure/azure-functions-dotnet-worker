// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    /// <summary>
    /// Samples demonstrating binding to the <see cref="CloudEvent"/> type.
    /// </summary>
    public class CloudEventSamples
    {
        private readonly ILogger<CloudEventSamples> _logger;

        public CloudEventSamples(ILogger<CloudEventSamples> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// This function demonstrates binding to a single <see cref="CloudEvent"/>.
        /// </summary>
        [Function(nameof(CloudEventFunction))]
        public void CloudEventFunction([EventGridTrigger] CloudEvent cloudEvent)
        {
            _logger.LogInformation("Event type: {type}, Event subject: {subject}", cloudEvent.Type, cloudEvent.Subject);
        }

        /// <summary>
        /// This function demonstrates binding to an array of <see cref="CloudEvent"/>.
        /// Note that when doing so, you must also set the <see cref="EventGridTriggerAttribute.IsBatched"/> property
        /// to <value>true</value>.
        /// </summary>
        [Function(nameof(CloudEventBatchFunction))]
        public void CloudEventBatchFunction([EventGridTrigger(IsBatched = true)] CloudEvent[] cloudEvents)
        {
            foreach (var cloudEvent in cloudEvents)
            {
                _logger.LogInformation("Event type: {type}, Event subject: {subject}", cloudEvent.Type, cloudEvent.Subject);
            }
        }
    }
}
