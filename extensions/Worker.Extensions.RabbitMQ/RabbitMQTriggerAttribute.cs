// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    public sealed class RabbitMQTriggerAttribute : TriggerBindingAttribute
    {
        public RabbitMQTriggerAttribute(string queueName)
        {
            QueueName = queueName;
        }

        public RabbitMQTriggerAttribute(string hostName, string userNameSetting, string passwordSetting, int port, string queueName)
        {
            HostName = hostName;
            UserNameSetting = userNameSetting;
            PasswordSetting = passwordSetting;
            Port = port;
            QueueName = queueName;
        }

        /// <summary>
        /// Gets or sets the name of app setting that contains the connection string to authenticate with RabbitMQ.
        /// </summary>
        public string? ConnectionStringSetting { get; set; }

        /// <summary>
        /// Gets or sets the HostName used to authenticate with RabbitMQ.
        /// This is ignored if 'ConnectionStringSetting' is set.
        /// </summary>
        public string? HostName { get; set; }

        /// <summary>
        /// Gets the QueueName to receive messages from.
        /// </summary>
        public string QueueName { get; }

        /// <summary>
        /// Gets or sets the name of the app setting containing the username to authenticate with RabbitMQ.
        /// The app setting name needs to be enclosed between %%. Eg: { UserNameSetting: "%UserNameFromSettings%" }.
        /// This is ignored if 'ConnectionStringSetting' is set.
        /// </summary>
        public string? UserNameSetting { get; set; }

        /// <summary>
        /// Gets or sets the name of the app setting containing the password to authenticate with RabbitMQ.
        /// The app setting name needs to be enclosed between %%. Eg: { PasswordSetting: "%PasswordFromSettings%" }.
        ///  This is ignored if 'ConnectionStringSetting' is set.
        /// </summary>
        public string? PasswordSetting { get; set; }

        /// <summary>
        /// Gets or sets the Port used. Defaults to 0.
        /// </summary>
        public int Port { get; set; }
    }
}
