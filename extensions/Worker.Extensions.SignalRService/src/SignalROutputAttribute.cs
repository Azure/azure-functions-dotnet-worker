// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Attribute used to specify the attribute target should output its data to SignalR Service.
    /// </summary>
    /// <remarks>
    /// To send a message, the target type should have following structure:
    /// <code>
    /// public class SignalRMessage
    /// {
    ///     public string ConnectionId { get; set; }
    ///     public string UserId { get; set; }
    ///     public string GroupName { get; set; }
    ///     public string Target { get; set; }  //required
    ///     public object[] Arguments { get; set; }  //required
    ///     public ServiceEndpoint[] Endpoints { get; set; }  // null to send to all endpoints
    /// }
    /// </code>
    /// To manage users or connections in a group, the target type should have following structure:
    /// <code>
    /// public class SignalRGroupAction
    /// {
    ///     public string ConnectionId { get; set; }
    ///     public string UserId { get; set; }
    ///     public string GroupName { get; set; }
    ///     public GroupAction Action { get; set; }  // enum type, valid values contain "Add", "Remove", "RemoveAll".
    ///     public ServiceEndpoint[] Endpoints { get; set; }  // null to send to all endpoints
    /// }
    /// </code>
    /// The `ServiceEndpoint` value in the above structures should be got and filtered from the target of <see cref="SignalREndpointsInputAttribute"/>.
    /// </remarks>
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
