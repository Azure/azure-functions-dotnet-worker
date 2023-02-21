﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Net;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Web PubSub service request context.
    /// </summary>
    [JsonConverter(typeof(WebPubSubContextJsonConverter))]
    public sealed class WebPubSubContext
    {
        /// <summary>
        /// Request body.
        /// </summary>
        [JsonPropertyName("request")]
        public WebPubSubEventRequest Request { get; set; }

        /// <summary>
        /// System build response for easy return, works for AbuseProtection and Errors.
        /// </summary>
        [JsonPropertyName("response")]
        public SimpleResponse Response { get; set; }

        /// <summary>
        /// Error detail message.
        /// </summary>
        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Flag to indicate whether the request has error.
        /// </summary>
        [JsonPropertyName("hasError")]
        public bool HasError { get; set; }

        /// <summary>
        /// Flag to indicate if it's a validation request.
        /// </summary>
        [JsonPropertyName("isPreflight")]
        public bool IsPreflight { get; set; }
    }

    public sealed class SimpleResponse
    {
        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("headers")]
        public Dictionary<string, string[]> Headers { get; set; }
    }
}
