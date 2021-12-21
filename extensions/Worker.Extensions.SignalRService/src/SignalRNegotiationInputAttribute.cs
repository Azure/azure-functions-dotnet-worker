// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Attribute used to bind SignalR negotiation context to a parameter, which provides information to choose an available SignalR endpoint and corresponding connection info for a SignalR client to connect to SignalR Service.
    /// </summary>
    /// <remarks>
    /// The target object can have following attributes:
    /// <code>
    /// public class NegotiationContext
    /// {
    ///     public EndpointConnectionInfo[] Endpoints { get; set; }
    /// }
    /// public class EndpointConnectionInfo
    /// {
    ///     public EndpointType EndpointType { get; set; }  // enum type, "Primary" or "Secondary"
    ///     public string Name { get; set; }
    ///     public string Endpoint { get; set; }
    ///     public bool Online { get; set; }
    ///     public SignalRConnectionInfo ConnectionInfo { get; set; }
    /// }
    /// public class SignalRConnectionInfo
    /// {
    ///     public string Url { get; set; }
    ///     public string AccessToken { get; set; }
    /// }
    /// </code>
    /// </remarks>
    public sealed class SignalRNegotiationInputAttribute : InputBindingAttribute
    {
        /// <summary>
        /// Gets or sets the app setting name that contains the Azure SignalR connection string.
        /// </summary>
        public string? ConnectionStringSetting { get; set; }

        /// <summary>
        /// Gets or sets the name of the hub to which the SignalR client is going to connect.
        /// </summary>
        public string? HubName { get; set; }

        /// <summary>
        /// Gets or sets the user id assigned to the SignalR client.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Gets or sets the JWT token whose claims will be added to the user claims.
        /// </summary>
        public string? IdToken { get; set; }

        /// <summary>
        /// Gets or sets the claim type list used to filter the claims in the <see cref="IdToken"/>.
        /// </summary>
        public string[]? ClaimTypeList { get; set; }

        public SignalRNegotiationInputAttribute(string hubName, string connectionStringSetting)
        {
            HubName = hubName;
            ConnectionStringSetting = connectionStringSetting;
        }
    }
}
