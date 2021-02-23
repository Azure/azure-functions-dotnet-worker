// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.ServiceBus;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class ServiceBusFunction
    {
        [Function("ServiceBusFunction")]
        [ServiceBusOutput("output", "outputQueue", Connection = "ServiceBusConnection")]
        public static void Run([ServiceBusTrigger("queue", Connection = "ServiceBusConnection")] string item,
            FunctionContext context)
        {
            var logger = context.GetLogger("ServiceBusFunction");

            logger.LogInformation(item);

            var message = $"Output message created at {DateTime.Now}";
            context.OutputBindings["output"] = message;
        }
    }
}
