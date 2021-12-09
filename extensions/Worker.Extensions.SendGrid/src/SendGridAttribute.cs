// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendGridOutputAttribute"/> class.
    /// </summary>
    public sealed class SendGridOutputAttribute : OutputBindingAttribute
    {
        /// <summary>
        /// Initialize a new instance of the <see cref="SendGridOutputAttribute"/>
        /// </summary>
        public SendGridOutputAttribute()
        {
        }

        /// <summary>
        /// Gets or sets an optional string value indicating the app setting to use as the SendGrid API key, 
        /// if different than the one specified in the <see cref="SendGrid.SendGridClientOptions"/>.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the message "To" field. May include binding parameters.
        /// </summary>
        public string? To { get; set; }

        /// <summary>
        /// Gets or sets the message "From" field. May include binding parameters.
        /// </summary>
        public string? From { get; set; }

        /// <summary>
        /// Gets or sets the message "Subject" field. May include binding parameters.
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// Gets or sets the message "Text" field. May include binding parameters.
        /// </summary>
        public string? Text { get; set; }
    }
}
