// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.EventHubs
{
    public sealed class EventHubOutputAttribute : OutputBindingAttribute
    {
        /// <summary>
        /// Initialize a new instance of the <see cref="EventHubOutputAttribute"/>
        /// </summary>
        /// <param name="name">The name of the output binding property to bind.</param>
        /// <param name="eventHubName">Name of the event hub </param>
        public EventHubOutputAttribute(string name, string eventHubName) : base(name)
        {
            EventHubName = eventHubName;
        }

        /// <summary>
        /// The name of the event hub.
        /// </summary>
        public string EventHubName { get; private set; }

        /// <summary>
        /// Gets or sets the optional connection string name that contains the Event Hub connection string. If missing, tries to use a registered event hub sender.
        /// </summary>
        public string? Connection { get; set; }
    }
}
