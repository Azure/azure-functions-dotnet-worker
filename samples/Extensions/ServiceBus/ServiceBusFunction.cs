// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class ServiceBusFunction
    {
        [Function("ServiceBusFunction")]
        [ServiceBusOutput("outputQueue", Connection = "ServiceBusConnection")]
        public static string Run([ServiceBusTrigger("queue", Connection = "ServiceBusConnection")] string item,
            FunctionContext context)
        {
            var logger = context.GetLogger("ServiceBusFunction");

            logger.LogInformation(item);

            var message = $"Output message created at {DateTime.Now}";
            return message;
        }
    }
}
