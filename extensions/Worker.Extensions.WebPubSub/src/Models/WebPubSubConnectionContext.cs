// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Request context from headers following CloudEvents.
    /// </summary>
    public sealed class WebPubSubConnectionContext
    {
        /// <summary>
        /// The type of the message.
        /// </summary>
        public WebPubSubEventType EventType { get; set; }

        /// <summary>
        /// The event name of the message.
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// The hub which the connection belongs to.
        /// </summary>
        public string Hub { get; set; }

        /// <summary>
        /// The connection-id of the client.
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        /// The user identity of the client.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// The signature for validation.
        /// </summary>
        public string Signature { get; set; }

        /// <summary>
        /// Upstream origin.
        /// </summary>
        public string Origin { get; set; }

        /// <summary>
        /// The connection states.
        /// </summary>
        [JsonConverter(typeof(ConnectionStatesConverter))]
        public IReadOnlyDictionary<string, BinaryData> ConnectionStates { get; set; }

        /// <summary>
        /// The headers of request.
        /// </summary>
        public IReadOnlyDictionary<string, string[]> Headers { get; set; }
    }
}
