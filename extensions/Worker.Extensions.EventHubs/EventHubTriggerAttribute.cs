// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    public sealed class EventHubTriggerAttribute : TriggerBindingAttribute, IBatchedInput
    {
        /// <summary>
        /// Create an instance of this attribute.
        /// </summary>
        /// <param name="eventHubName">Event hub to listen on for messages. </param>
        public EventHubTriggerAttribute(string eventHubName, bool IsBatched = true)
        {
            EventHubName = eventHubName;
            this.IsBatched = IsBatched;
        }

        /// <summary>
        /// Name of the event hub.
        /// </summary>
        public string EventHubName { get; private set; }

        /// <summary>
        /// Optional Name of the consumer group. If missing, then use the default name, "$Default"
        /// </summary>
        public string? ConsumerGroup { get; set; }

        /// <summary>
        /// Gets or sets the optional app setting name that contains the Event Hub connection string. If missing, tries to use a registered event hub receiver.
        /// </summary>
        public string? Connection { get; set; }

        /// <summary>
        /// Configures trigger to process events in batches or one at a time. Default value is "true".
        /// </summary>
        public bool IsBatched { get; set; }
    }
}
