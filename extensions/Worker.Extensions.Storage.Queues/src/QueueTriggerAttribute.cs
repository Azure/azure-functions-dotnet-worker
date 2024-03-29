﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    [InputConverter(typeof(QueueMessageConverter))]
    [InputConverter(typeof(QueueMessageBinaryDataConverter))]
    [ConverterFallbackBehavior(ConverterFallbackBehavior.Default)]
    public sealed class QueueTriggerAttribute : TriggerBindingAttribute
    {
        private readonly string _queueName;

        public QueueTriggerAttribute(string queueName)
        {
            _queueName = queueName;
        }

        /// <summary>Gets the name of the queue to which to bind.</summary>
        public string QueueName
        {
            get { return _queueName; }
        }

        /// <summary>
        /// Gets or sets the app setting name that contains the Azure Storage connection string.
        /// </summary>
        public string? Connection { get; set; }
    }
}
