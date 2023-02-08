// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net.Http;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Web PubSub service request context.
    /// </summary>
    public sealed class WebPubSubContext
    {
        /// <summary>
        /// Request body.
        /// </summary>
        public WebPubSubEventRequest Request { get; }

        /// <summary>
        /// System build response for easy return, works for AbuseProtection and Errors.
        /// </summary>
        public HttpResponseMessage Response { get; }

        /// <summary>
        /// Error detail message.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Flag to indicate whether the request has error.
        /// </summary>
        public bool HasError { get; }

        /// <summary>
        /// Flag to indicate if it's a validation request.
        /// </summary>
        public bool IsPreflight { get; }
    }
}
