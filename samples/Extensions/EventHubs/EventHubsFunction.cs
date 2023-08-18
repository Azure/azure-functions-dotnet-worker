// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public class EventHubsFunction
    {
        private readonly ILogger<EventHubsFunction> _logger;

        public EventHubsFunction(ILogger<EventHubsFunction> logger)
        {
            _logger = logger;
        }

        [Function(nameof(EventHubFunction))]
        [FixedDelayRetry(5, "00:00:10")]
        [EventHubOutput("dest", Connection = "EventHubConnection")]
        public string EventHubFunction(
            [EventHubTrigger("src", Connection = "EventHubConnection")] string[] input,
            FunctionContext context)
        {
            _logger.LogInformation("First Event Hubs triggered message: {msg}", input[0]);

            var message = $"Output message created at {DateTime.Now}";
            return message;
        }

        /// <summary>
        /// This function demonstrates binding to a single <see cref="EventData"/>. Note that when doing so, you must also set the
        /// <see cref="EventHubTriggerAttribute.IsBatched"/> property to <value>false</value> as the default value of this property is
        /// <value>true</value>.
        /// </summary>
        [Function(nameof(EventDataFunctions))]
        public void EventDataFunctions(
            [EventHubTrigger("queue", Connection = "EventHubConnection", IsBatched = false)] EventData @event)
        {
            _logger.LogInformation("Event Body: {body}", @event.Body);
            _logger.LogInformation("Event Content-Type: {contentType}", @event.ContentType);
        }

        /// <summary>
        /// This function demonstrates binding to an array of <see cref="EventData"/>.
        /// </summary>
        [Function(nameof(EventDataBatchFunction))]
        public void EventDataBatchFunction(
            [EventHubTrigger("queue", Connection = "EventHubConnection")] EventData[] events)
        {
            foreach (EventData @event in events)
            {
                _logger.LogInformation("Event Body: {body}", @event.Body);
                _logger.LogInformation("Event Content-Type: {contentType}", @event.ContentType);
            }
        }

        /// <summary>
        /// This functions demonstrates that it is possible to bind to both the <see cref="EventData"/> and any of the supported binding contract
        /// properties at the same time. If attempting this, the <see cref="EventData"/> must be the first parameter. There is not
        /// much benefit to doing this as all of the binding contract properties are available as properties on the <see cref="EventData"/>.
        /// </summary>
        [Function(nameof(EventDataWithStringPropertiesFunction))]
        public void EventDataWithStringPropertiesFunction(
            [EventHubTrigger("queue", Connection = "EventHubConnection", IsBatched = false)]
            EventData @event, string contentType, long offset)
        {
            // The ContentType property and the contentType parameter are the same.
            _logger.LogInformation("Event Content-Type: {contentType}", @event.ContentType);
            _logger.LogInformation("Event Content-Type: {contentType}", contentType);

            // Similarly the Offset property and the offset parameter are the same.
            _logger.LogInformation("Event offset: {offset}", @event.Offset);
            _logger.LogInformation("Event offset: {offset}", offset);
        }
    }
}
