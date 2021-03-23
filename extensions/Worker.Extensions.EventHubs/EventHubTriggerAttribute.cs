// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Attribute used to mark a function that should be triggered by Event Hubs messages.
    /// </summary>
    public sealed class EventHubTriggerAttribute : TriggerBindingAttribute, ISupportCardinality
    {
        // Batch by default
        private bool _isBatched = true;

        /// <summary>
        /// Create an instance of this attribute.
        /// </summary>
        /// <param name="eventHubName">Event hub to listen on for messages. </param>
        public EventHubTriggerAttribute(string eventHubName)
        {
            EventHubName = eventHubName;
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
        /// Gets or sets the configuration to enable batch processing of events. Default value is "true".
        /// </summary>
        [DefaultValue(true)]
        public bool IsBatched
        {
            get => _isBatched;
            set => _isBatched = value;
        }

        Cardinality ISupportCardinality.Cardinality
        {
            get
            {
                if (_isBatched)
                {
                    return Cardinality.Many;
                }
                else
                {
                    return Cardinality.One;
                }
            }
            set
            {
                if (value.Equals(Cardinality.Many))
                {
                    _isBatched = true;
                }
                else
                {
                    _isBatched = false;
                }
            }
        }
    }
}
