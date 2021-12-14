// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Attribute used to bind a list of SignalR Service endpoints to the parameter.
    /// </summary>
    /// <remarks>
    /// The SignalR Service endpoint can have following properties:
    /// <code>
    /// public class ServiceEndpoint
    /// {
    ///     public EndpointType EndpointType { get; set; } //enum type, "Primary" or "Secondary"
    ///     public string Name { get; set; }
    ///     public string Endpoint { get; set; }
    ///     public bool Online { get; set; }
    /// }
    /// </code>
    /// </remarks>
    public sealed class SignalREndpointsInputAttribute : InputBindingAttribute
    {
        public SignalREndpointsInputAttribute(string hubName)
        {
            HubName = hubName;
        }

        public SignalREndpointsInputAttribute(string hubName, string connectionStringSetting)
        {
            HubName = hubName;
            ConnectionStringSetting = connectionStringSetting;
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
