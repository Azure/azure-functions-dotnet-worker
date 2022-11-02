// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Extensions.SignalR
{
    public static class SignalRNegotiationFunctions
    {
        [Function("Negotiate")]
        public static SignalRConnectionInfo Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            [SignalRConnectionInfoInput(HubName = "chat", ConnectionStringSetting = "SignalRConnection")] SignalRConnectionInfo connectionInfo)
        {
            return connectionInfo;
        }

        // When you have multiple SignalR service instances and you want to customize the rule that route a client
        [Function("NegotiateWithMultipleEndpoints")]
        public static SignalRConnectionInfo NegotiateWithMultipleEndpoints(
            [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequestData req,
            [SignalRNegotiationInput("chatHub", "SignalRConnection")] SignalRNegotiationContext negotiationContext)
        {
            // customize your rule here
            return negotiationContext.Endpoints[0].ConnectionInfo;
        }
    }
}
