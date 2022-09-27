// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Extensions.SignalR
{
    public static class SignalRTriggerFunctions
    {
        [Function("OnConnected")]
        public static void OnConnected([SignalRTrigger("chat", "connections", "connected", ConnectionStringSetting = "SignalRConnection")] SignalRInvocationContext invocationContext, FunctionContext functionContext)
        {
            var logger = functionContext.GetLogger("OnConnected");
            logger.LogInformation($"Connection {invocationContext.ConnectionId} is connected");
        }

        [Function("OnDisconnected")]
        public static void OnDisconnected([SignalRTrigger("chat", "connections", "disconnected", ConnectionStringSetting = "SignalRConnection")] SignalRInvocationContext invocationContext, FunctionContext functionContext)
        {
            var logger = functionContext.GetLogger("OnDisconnected");
            logger.LogInformation($"Connection {invocationContext.ConnectionId} is disconnected. Error: {invocationContext.Error}");
        }

        [Function("OnClientMessage")]
        public static void OnClientMessage([SignalRTrigger("Hub", "messages", "sendMessage", "content", ConnectionStringSetting = "SignalRConnection")] SignalRInvocationContext invocationContext, string content, FunctionContext functionContext)
        {
            var logger = functionContext.GetLogger("OnClientMessage");
            logger.LogInformation($"Connection {invocationContext.ConnectionId} sent a message. Message content: {content}");
        }
    }
}
