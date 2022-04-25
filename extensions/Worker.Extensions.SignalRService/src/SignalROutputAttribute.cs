// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Binds to a <see cref="SignalRMessageAction"/> to send a message.
    /// Binds to a <see cref="SignalRGroupAction"/> to manage a group.
    /// </summary>
    public sealed class SignalROutputAttribute : OutputBindingAttribute
    {
        public SignalROutputAttribute()
        {
        }

        /// <summary>
        /// Gets or sets the app setting name that contains the Azure SignalR connection string.
        /// </summary>
        public string? ConnectionStringSetting { get; set; }

        /// <summary>
        /// Gets or sets the hub name.
        /// </summary>
        public string? HubName { get; set; }
    }
}
