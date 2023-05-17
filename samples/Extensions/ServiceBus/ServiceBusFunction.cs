// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Azure.Messaging.ServiceBus;
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

        [Function("ServiceBusSdkTypeBindingFunction")]
        [ServiceBusOutput("outputQueue", Connection = "ServiceBusConnection")]
        public static string Run([ServiceBusTrigger("queue", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message, FunctionContext context)
        {
            var logger = context.GetLogger("ServiceBusFunction");

            logger.LogInformation($"Message body: {message.Body}");
            logger.LogInformation($"Message ID: {message.MessageId}");

            return $"Output message created at {DateTime.Now}";
        }
    }
}
