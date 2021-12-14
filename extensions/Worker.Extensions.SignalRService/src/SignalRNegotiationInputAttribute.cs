// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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
    public sealed class SignalRNegotiationInputAttribute : SignalRNegotiationBaseAttribute
    {
        public SignalRNegotiationInputAttribute(string hubName, string connectionStringSetting)
        {
            HubName = hubName;  
            ConnectionStringSetting = connectionStringSetting;
        }
    }
}
