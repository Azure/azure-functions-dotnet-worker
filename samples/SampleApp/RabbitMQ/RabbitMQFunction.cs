// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.RabbitMQ;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class RabbitMQFunction
    {
        [Function("RabbitMQFunction")]
        [RabbitMQOutput("rabbitOutput", QueueName = "destinationQueue", ConnectionStringSetting = "rabbitMQConnectionAppSetting")]
        public static void Run([RabbitMQTrigger("queue", ConnectionStringSetting = "rabbitMQConnectionAppSetting")] string item,
            FunctionContext context)
        {
            var logger = context.GetLogger("RabbitMQFunction");

            logger.LogInformation(item);

            var message = $"Output message created at {DateTime.Now}";
            context.OutputBindings["rabbitOutput"] = message;
        }
    }
}
