﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// The context for a SignalR invocation.
    /// </summary>
    public sealed class SignalRInvocationContext
    {
        /// <summary>
        /// The arguments of the invocation.
        /// </summary>
        public object[]? Arguments { get; set; }

        /// <summary>
        /// The error message of connections disconnected event.
        /// Only connections disconnected event can have this property, and it can be empty if the connection is closed with no error.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// The category of the invocation.
        /// </summary>
        public SignalRInvocationCategory Category { get; set; }

        /// <summary>
        ///  For <see cref="SignalRInvocationCategory.Messages"/> category, the event is the target in invocation message sent from clients. 
        ///  For <see cref="SignalRInvocationCategory.Connections"/> category, only "connected" and "disconnected" are used.
        /// </summary>
        public string Event { get; set; }

        /// <summary>
        /// The hub the invocation belongs to.
        /// </summary>
        public string Hub { get; set; }

        /// <summary>
        /// The connection ID of the client which triggers the invocation.
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        /// The user ID of the client which triggers the invocation.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// The headers of request.
        /// Headers with duplicated key will be joined by comma.
        /// </summary>
        public IDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// The query of the request when client connect to the service.
        /// Queries with duplicated key will be joined by comma.
        /// </summary>
        public IDictionary<string, string> Query { get; set; }

        /// <summary>
        /// The claims of the client.
        /// If you multiple claims have the same key, only the first one will be reserved.
        /// </summary>
        public IDictionary<string, string> Claims { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SignalRInvocationCategory
    {
        Connections,
        Messages
    }
}
