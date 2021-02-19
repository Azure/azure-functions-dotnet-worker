// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.SignalRService;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class SignalRFunction
    {
        [Function("SignalRFunction")]
        [SignalROutput("output", HubName = "chat", ConnectionStringSetting = "SignalRConnectionString")]
        public static void Run([SignalRTrigger("SignalRTest", "messages", "SendMessage", parameterNames: new string[] { "message" },
            ConnectionStringSetting = "SignalRConnectionString")] string item,
            [SignalRConnectionInfoInput(HubName = "chat")] MyConnectionInfo connectionInfo,
            FunctionContext context)
        {
            var logger = context.GetLogger("SignalRFunction");

            logger.LogInformation(item);
            logger.LogInformation($"Connection URL = {connectionInfo.Url}");

            var message = $"Output message created at {DateTime.Now}";
            context.OutputBindings["output"] = new MyMessage()
            {
                Target = "newMessage",
                Arguments = new[] { message }
            };
        }
    }

    public class MyConnectionInfo
    {
        public string Url { get; set; }

        public string AccessToken { get; set; }
    }

    public class MyMessage
    {
        public string Target { get; set; }

        public object[] Arguments { get; set; }
    }
}
