// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker
{
    public abstract class WebPubSubEventRequest
    {
        /// <summary>
        /// Connection context contains connection metadata following CloudEvents.
        /// </summary>
        [JsonPropertyName("connectionContext")]
        public WebPubSubConnectionContext ConnectionContext { get; set; }
    }
}
