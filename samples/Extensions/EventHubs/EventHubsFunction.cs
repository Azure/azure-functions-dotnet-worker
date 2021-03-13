// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class EventHubsFunction
    {
        [Function("EventHubsFunction")]
        [EventHubOutput("dest", Connection = "EventHubConnectionAppSetting")]
        public static string Run([EventHubTrigger("src", Connection = "EventHubConnectionAppSetting")] string[] input,
            FunctionContext context)
        {
            var logger = context.GetLogger("EventHubsFunction");

            logger.LogInformation($"First Event Hubs triggered message: {input[0]}");

            var message = $"Output message created at {DateTime.Now}";
            return message;
        }
    }
}
