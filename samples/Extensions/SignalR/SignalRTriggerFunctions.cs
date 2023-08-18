// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Extensions.SignalR
{
    public static class SignalRTriggerFunctions
    {
        // <snippet_on_connected>
        [Function(nameof(OnConnected))]
        public static void OnConnected(
            [SignalRTrigger("chat", "connections", "connected", ConnectionStringSetting = "SignalRConnection")]
                SignalRInvocationContext invocationContext, FunctionContext functionContext)
        {
            var logger = functionContext.GetLogger(nameof(OnConnected));
            logger.LogInformation("Connection {connectionId} is connected", invocationContext.ConnectionId);
        }
        // </snippet_on_connected>

        // <snippet_on_disconnected>
        [Function(nameof(OnDisconnected))]
        public static void OnDisconnected(
            [SignalRTrigger("chat", "connections", "disconnected", ConnectionStringSetting = "SignalRConnection")]
                SignalRInvocationContext invocationContext, FunctionContext functionContext)
        {
            var logger = functionContext.GetLogger(nameof(OnDisconnected));
            logger.LogInformation("Connection {connectionId} is disconnected. Error: {error}", invocationContext.ConnectionId, invocationContext.Error);
        }
        // </snippet_on_disconnected>

        // <snippet_on_message>
        [Function(nameof(OnClientMessage))]
        public static void OnClientMessage(
            [SignalRTrigger("Hub", "messages", "sendMessage", "content", ConnectionStringSetting = "SignalRConnection")]
                SignalRInvocationContext invocationContext, string content, FunctionContext functionContext)
        {
            var logger = functionContext.GetLogger(nameof(OnClientMessage));
            logger.LogInformation("Connection {connectionId} sent a message. Message content: {content}", invocationContext.ConnectionId, content);
        }
        // </snippet_on_message>
    }
}
