// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    /// <summary>
    /// Samples demonstrating binding to the <see cref="EventGridEvent"/> type.
    /// </summary>
    public class EventGridEventSamples
    {
        private readonly ILogger<EventGridEventSamples> _logger;

        public EventGridEventSamples(ILogger<EventGridEventSamples> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// This function demonstrates binding to a single <see cref="EventGridEvent"/>.
        /// </summary>
        [Function(nameof(EventGridEventFunction))]
        public void EventGridEventFunction([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            _logger.LogInformation("Event received: {event}", eventGridEvent.Data.ToString());
        }

        /// <summary>
        /// This function demonstrates binding to an array of <see cref="EventGridEvent"/>.
        /// Note that when doing so, you must also set the <see cref="EventGridTriggerAttribute.IsBatched"/> property
        /// to <value>true</value>.
        /// </summary>
        [Function(nameof(EventGridEventBatchFunction))]
        public void EventGridEventBatchFunction([EventGridTrigger(IsBatched = true)] EventGridEvent[] eventGridEvents)
        {
            foreach (var eventGridEvent in eventGridEvents)
            {
                _logger.LogInformation("Event received: {event}", eventGridEvent.Data.ToString());
            }
        }
    }
}
