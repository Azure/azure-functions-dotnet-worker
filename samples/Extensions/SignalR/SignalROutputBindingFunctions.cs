// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace SampleApp
{
    public static class SignalROutputBindingFunctions
    {
        [Function(nameof(BroadcastToAll))]
        [SignalROutput(HubName = "chat", ConnectionStringSetting = "SignalRConnection")]
        public static SignalRMessageAction BroadcastToAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            using var bodyReader = new StreamReader(req.Body);
            return new SignalRMessageAction("newMessage")
            {
                // broadcast to all the connected clients without specifying any connection, user or group.
                Arguments = new[] { bodyReader.ReadToEnd() },
            };
        }

        [Function(nameof(SendToConnection))]
        [SignalROutput(HubName = "chat", ConnectionStringSetting = "SignalRConnection")]
        public static SignalRMessageAction SendToConnection(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            using var bodyReader = new StreamReader(req.Body);
            return new SignalRMessageAction("newMessage")
            {
                Arguments = new[] { bodyReader.ReadToEnd() },
                ConnectionId = "connectionToSend",
            };
        }

        [Function(nameof(SendToUser))]
        [SignalROutput(HubName = "chat", ConnectionStringSetting = "SignalRConnection")]
        public static SignalRMessageAction SendToUser([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            using var bodyReader = new StreamReader(req.Body);
            return new SignalRMessageAction("newMessage")
            {
                Arguments = new[] { bodyReader.ReadToEnd() },
                UserId = "userToSend",
            };
        }

        [Function(nameof(SendToGroup))]
        [SignalROutput(HubName = "chat", ConnectionStringSetting = "SignalRConnection")]
        public static SignalRMessageAction SendToGroup([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            using var bodyReader = new StreamReader(req.Body);
            return new SignalRMessageAction("newMessage")
            {
                Arguments = new[] { bodyReader.ReadToEnd() },
                GroupName = "groupToSend"
            };
        }

        [Function(nameof(SendToEndpoint))]
        [SignalROutput(HubName = "chat", ConnectionStringSetting = "SignalRConnection")]
        public static SignalRMessageAction SendToEndpoint(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            [SignalREndpointsInput("chat", ConnectionStringSetting = "SignalRConnection")] SignalREndpoint[] endpoints)
        {
            using var bodyReader = new StreamReader(req.Body);
            return new SignalRMessageAction("newMessage")
            {
                Arguments = new[] { bodyReader.ReadToEnd() },
                // Only send to primary endpoint if you have configured multiple SignalR Service instances.
                // The use of 'Endpoints' can be combined with other properties such as UserId, GroupName, ConnectionID.
                Endpoints = endpoints.Where(e => e.EndpointType == SignalREndpointType.Primary).ToArray()
            };
        }

        [Function(nameof(RemoveFromGroup))]
        [SignalROutput(HubName = "chat", ConnectionStringSetting = "SignalRConnection")]
        public static SignalRGroupAction RemoveFromGroup([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            return new SignalRGroupAction(SignalRGroupActionType.Remove)
            {
                GroupName = "group1",
                UserId = "user1"
            };
        }
    }
}
