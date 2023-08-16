// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Extensions.SignalR
{
    public static class SignalRNegotiationFunctions
    {
        // <snippet_negotiate>
        [Function(nameof(Negotiate))]
        public static string Negotiate([HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequestData req,
            [SignalRConnectionInfoInput(HubName = "serverless")] string connectionInfo)
        {
            // The serialization of the connection info object is done by the framework. It should be camel case. The SignalR client respects the camel case response only.
            return connectionInfo;
        }
        // </snippet_negotiate>


        // When you have multiple SignalR service instances and you want to customize the rule that route a client
        // <snippet_negotiate_multiple_endpoint>
        public static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        [Function(nameof(NegotiateWithMultipleEndpoints))]
        public static string NegotiateWithMultipleEndpoints(
            [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequestData req,
            [SignalRNegotiationInput("chatHub", "SignalRConnection")] SignalRNegotiationContext negotiationContext)
        {
            // Customize your rule here
            var connectionInfo = negotiationContext.Endpoints[0].ConnectionInfo;
            // The SignalR client respects the camel case response only.
            return JsonSerializer.Serialize(connectionInfo, SerializerOptions);
        }
        // </snippet_negotiate_multiple_endpoint>

    }
}
