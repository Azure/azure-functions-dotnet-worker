// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    public sealed class EventGridOutputAttribute : OutputBindingAttribute
    {
        /// <summary>
        /// Initialize a new instance of the <see cref="EventGridOutputAttribute"/>
        /// </summary>
        public EventGridOutputAttribute()
        {
        }

        /// <summary>Gets or sets the topic events endpoint URI. Eg: https://topic1.westus2-1.eventgrid.azure.net/api/events 
        /// This is found in the Event Grid Topic's definition. You can find information on getting the Url for a topic here: https://docs.microsoft.com/en-us/azure/event-grid/custom-event-quickstart#send-an-event-to-your-topic
        /// </summary>
        public string? TopicEndpointUri { get; set; }

        /// <summary>Gets or sets the Topic Key setting. You can find information on getting the Key for a topic here: https://docs.microsoft.com/en-us/azure/event-grid/custom-event-quickstart#send-an-event-to-your-topic </summary>
        public string? TopicKeySetting { get; set; }
    }
}
