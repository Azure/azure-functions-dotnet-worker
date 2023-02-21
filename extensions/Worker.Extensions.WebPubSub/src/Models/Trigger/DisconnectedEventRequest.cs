// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Disconnected event request.
    /// </summary>
    public sealed class DisconnectedEventRequest : WebPubSubEventRequest
    {
        /// <summary>
        /// Reason of the disconnect event.
        /// </summary>
        [JsonPropertyName("reason")]
        public string Reason { get; set; }
    }
}
