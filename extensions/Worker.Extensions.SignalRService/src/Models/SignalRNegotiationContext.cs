// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// The context of SignalR client negotiation.
    /// </summary>
    public sealed class SignalRNegotiationContext
    {
        /// <summary>
        /// The SignalR endpoints with connection information to select.
        /// </summary>
        public SignalREndpointConnectionInfo[] Endpoints { get; set; }
    }

    /// <summary>
    /// Contains metadata of an SignalR endpoint and connection information for a client to connect to the SignalR Service.
    /// </summary>
    public sealed class SignalREndpointConnectionInfo
    {
        /// <summary>
        /// The type of an endpoint.
        /// </summary>
        public SignalREndpointType EndpointType { get; set; }

        /// <summary>
        /// The name of the endpoint.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The URL of the endpoint.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Whether the SignalR Service behind the endpoint is healthy.
        /// </summary>
        public bool Online { get; set; }

        /// <summary>
        /// The connection information for a client to connect to SignalR Service. Returns this field to the client if you choose this endpoint.
        /// </summary>
        public SignalRConnectionInfo ConnectionInfo { get; set; }
    }
}
