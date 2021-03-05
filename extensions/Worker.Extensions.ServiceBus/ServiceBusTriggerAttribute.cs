// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    public sealed class ServiceBusTriggerAttribute : TriggerBindingAttribute, ISupportCardinality
    {
        private bool _isBatched = false;

        private readonly string? _queueName;
        private readonly string? _topicName;
        private readonly string? _subscriptionName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusTriggerAttribute"/> class.
        /// </summary>
        /// <param name="queueName">The name of the queue to which to bind.</param>
        public ServiceBusTriggerAttribute(string queueName)
        {
            _queueName = queueName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusTriggerAttribute"/> class.
        /// </summary>
        /// <param name="topicName">The name of the topic to bind to.</param>
        /// <param name="subscriptionName">The name of the subscription in <paramref name="topicName"/> to bind to.</param>
        public ServiceBusTriggerAttribute(string topicName, string subscriptionName)
        {
            _topicName = topicName;
            _subscriptionName = subscriptionName;
        }

        /// <summary>
        /// Gets or sets the app setting name that contains the Service Bus connection string.
        /// </summary>
        public string? Connection { get; set; }

        /// <summary>
        /// Gets the name of the queue to which to bind.
        /// </summary>
        /// <remarks>When binding to a subscription in a topic, returns <see langword="null"/>.</remarks>
        public string? QueueName
        {
            get { return _queueName; }
        }

        /// <summary>
        /// Gets the name of the topic to which to bind.
        /// </summary>
        /// <remarks>When binding to a queue, returns <see langword="null"/>.</remarks>
        public string? TopicName
        {
            get { return _topicName; }
        }

        /// <summary>
        /// Gets the name of the subscription in <see cref="TopicName"/> to bind to.
        /// </summary>
        /// <remarks>When binding to a queue, returns <see langword="null"/>.</remarks>
        public string? SubscriptionName
        {
            get { return _subscriptionName; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the sessions are enabled.
        /// </summary>
        public bool IsSessionsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the configuration to enable batch processing of events. Default value is "false".
        /// </summary>
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
