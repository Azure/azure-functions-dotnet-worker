// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// An action to send a message to SignalR clients.
    /// </summary>
    public sealed class SignalRMessageAction
    {
        /// <summary>
        /// Initializes an instance of <see cref="SignalRMessageAction"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Throws if the target is null.</exception>
        public SignalRMessageAction(string target)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        /// <summary>
        /// Initializes an instance of <see cref="SignalRMessageAction"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Throws if the target is null.</exception>
        public SignalRMessageAction(string target, object[]? arguments) : this(target)
        {
            Arguments = arguments;
        }

        /// <summary>
        /// The connection ID to send to. Sets this field if you want to send a message to a single connection.
        /// </summary>
        public string? ConnectionId { get; set; }

        /// <summary>
        /// The user ID to send to. Sets this field if you want to send a message to a single user.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// The group to send to. Sets this field if you want to send a message to a group.
        /// </summary>
        public string? GroupName { get; set; }

        /// <summary>
        /// Required. The target to send to.
        /// </summary>
        public string Target { get; }

        /// <summary>
        /// The arguments to send.
        /// </summary>
        public object[]? Arguments { get; set; }

        /// <summary>
        /// The SignalR endpoints to send to. Leave it null if you want to send to all endpoints.
        /// You can get the available SignalR endpoints from the <see cref="SignalREndpointsInputAttribute"/> binding.
        /// </summary>
        public SignalREndpoint[]? Endpoints { get; set; }
    }
}
