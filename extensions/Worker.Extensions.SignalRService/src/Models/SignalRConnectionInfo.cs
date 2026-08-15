// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Contains necessary information for a SignalR client to connect to SignalR Service.
    /// </summary>
    public sealed class SignalRConnectionInfo
    {
        /// <summary>
        /// The URL for a client to connect to SignalR Service.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The access token for a client to connect to SignalR service.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// The access token lifetime in seconds. A refresh-aware client can use this to schedule its next authentication refresh.
        /// </summary>
        public int? TokenLifetimeSeconds { get; set; }
    }
}
