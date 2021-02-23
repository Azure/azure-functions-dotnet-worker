// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.EventHubs;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class EventHubsFunction
    {
        [Function("EventHubsFunction")]
        [EventHubOutput("myOutput", "dest", Connection = "EventHubConnectionAppSetting")]
        public static void Run([EventHubTrigger("src", Connection = "EventHubConnectionAppSetting")] string input,
            FunctionContext context)
        {
            var logger = context.GetLogger("EventHubsFunction");

            logger.LogInformation(input);

            var message = $"Output message created at {DateTime.Now}";
            context.OutputBindings["myOutput"] = message;
        }
    }
}
