// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Represents an endpoint of SignalR Service.
    /// </summary>
    public sealed class SignalREndpoint
    {
        /// <summary>
        /// Represents the priority of a SignalR endpoint for client routing. 
        /// The priorities are respected by the default router. 
        /// If you want to customize router rules, it's your business to handle this field.
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
        /// Indicates whether the SignalR Service behind the endpoint is healthy.
        /// </summary>
        public bool Online { get; set; }
    }

    /// <summary>
    /// Represents the priority of a SignalR endpoint for client routing. 
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SignalREndpointType
    {
        /// <summary>
        /// Preferred endpoints to receive client traffic. 
        /// </summary>
        Primary,

        /// <summary>
        /// Only receive client traffic when primary endpoints are not available.
        /// </summary>
        Secondary
    }
}
