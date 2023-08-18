// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace SampleApp
{
    /// <summary>
    /// The class is the same as SignalROutputBindingFunctions except the comments. Just keep the original one because the learn website refers to it.
    /// </summary>
    public static class SignalROutputBindingFunctions2
    {
        // <snippet_broadcast_to_all>
        [Function(nameof(BroadcastToAll))]
        [SignalROutput(HubName = "chat", ConnectionStringSetting = "SignalRConnection")]
        public static SignalRMessageAction BroadcastToAll([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            using var bodyReader = new StreamReader(req.Body);
            return new SignalRMessageAction("newMessage")
            {
                // broadcast to all the connected clients without specifying any connection, user or group.
                Arguments = new[] { bodyReader.ReadToEnd() },
            };
        }
        // </snippet_broadcast_to_all>

        // <snippet_send_to_connection>
        [Function(nameof(SendToConnection))]
        [SignalROutput(HubName = "chat", ConnectionStringSetting = "SignalRConnection")]
        public static SignalRMessageAction SendToConnection([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            using var bodyReader = new StreamReader(req.Body);
            return new SignalRMessageAction("newMessage")
            {
                Arguments = new[] { bodyReader.ReadToEnd() },
                ConnectionId = "connectionToSend",
            };
        }
        // </snippet_send_to_connection>

        // <snippet_send_to_user>
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
        // </snippet_send_to_user>

        // <snippet_send_to_group>
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
        // </snippet_send_to_group>

        // <snippet_send_to_endpoint>
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
        // </snippet_send_to_endpoint>

        // <snippet_remove_from_group>
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
        // </snippet_remove_from_group>
    }
}
