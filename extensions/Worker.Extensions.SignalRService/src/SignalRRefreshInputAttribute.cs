// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Refreshes the authentication of a live SignalR client connection without reconnecting and provides a
    /// <see cref="SignalRConnectionInfo"/> carrying the refreshed access token and its remaining lifetime.
    /// </summary>
    public sealed class SignalRRefreshInputAttribute : InputBindingAttribute
    {
        /// <summary>
        /// Gets or sets the app setting name that contains the Azure SignalR connection string.
        /// </summary>
        public string? ConnectionStringSetting { get; set; }

        /// <summary>
        /// Gets or sets the name of the hub to which the SignalR client is connected.
        /// </summary>
        public string? HubName { get; set; }

        /// <summary>
        /// Gets or sets the connection token of the live client connection to refresh.
        /// </summary>
        public string? ConnectionToken { get; set; }

        /// <summary>
        /// Gets or sets the new authentication lifetime, in seconds, applied to the connection.
        /// </summary>
        public int TokenLifetimeSeconds { get; set; }

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
    }
}
