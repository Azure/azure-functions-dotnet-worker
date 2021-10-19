// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    public sealed class ServiceBusOutputAttribute : OutputBindingAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusOutputAttribute"/> class.
        /// </summary>
        /// <param name="queueOrTopicName">The name of the queue or topic to bind to.</param>
        /// <param name="entityType">The type of the entity to bind to.</param>
        public ServiceBusOutputAttribute(string queueOrTopicName, ServiceBusEntityType entityType = ServiceBusEntityType.Queue)
        {
            QueueOrTopicName = queueOrTopicName;
            EntityType = entityType;
        }

        /// <summary>
        /// Gets the name of the queue or topic to bind to.
        /// </summary>
        public string QueueOrTopicName { get; private set; }

        /// <summary>
        /// Gets or sets the app setting name that contains the Service Bus connection string.
        /// </summary>
        public string? Connection { get; set; }

        /// <summary>
        /// Value indicating the type of the entity to bind to.
        /// </summary>
        public ServiceBusEntityType EntityType { get; set; }
    }
}
