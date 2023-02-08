// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

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
        public WebPubSubEventType EventType { get; }

        /// <summary>
        /// The event name of the message.
        /// </summary>
        public string EventName { get; }

        /// <summary>
        /// The hub which the connection belongs to.
        /// </summary>
        public string Hub { get; }

        /// <summary>
        /// The connection-id of the client.
        /// </summary>
        public string ConnectionId { get; }

        /// <summary>
        /// The user identity of the client.
        /// </summary>
        public string UserId { get; }

        /// <summary>
        /// The signature for validation.
        /// </summary>
        public string Signature { get; }

        /// <summary>
        /// Upstream origin.
        /// </summary>
        public string Origin { get; }

        /// <summary>
        /// The connection states.
        /// </summary>
        public IReadOnlyDictionary<string, BinaryData> ConnectionStates { get; }

        /// <summary>
        /// The headers of request.
        /// </summary>
        public IReadOnlyDictionary<string, string[]> Headers { get; }
    }
}
