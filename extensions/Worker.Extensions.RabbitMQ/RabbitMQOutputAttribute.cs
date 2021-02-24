// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    public sealed class RabbitMQOutputAttribute : OutputBindingAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitMQOutputAttribute"/> class.
        /// </summary>
        public RabbitMQOutputAttribute()
        {
        }

        /// <summary>
        /// Gets or sets the HostName used to authenticate with RabbitMQ.
        /// This is ignored if 'ConnectionStringSetting' is set.
        /// </summary>
        public string? HostName { get; set; }

        /// <summary>
        /// Gets or sets the QueueName to send messages to.
        /// </summary>
        public string? QueueName { get; set; }

        /// <summary>
        /// Gets or sets the name of the app setting containing the username to authenticate with RabbitMQ. Eg: { UserName: "UserNameFromSettings" }
        /// This is ignored if 'ConnectionStringSetting' is set.
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// Gets or sets the name of the app setting containing the password to authenticate with RabbitMQ. Eg: { Password: "PasswordFromSettings" }
        ///  This is ignored if 'ConnectionStringSetting' is set.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets the Port used. Defaults to 0.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the name of app setting that contains the connection string to authenticate with RabbitMQ.
        /// </summary>
        public string? ConnectionStringSetting { get; set; }
    }
}
