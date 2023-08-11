// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class RabbitMQFunction
    {
        [Function(nameof(RabbitMQFunction))]
        [RabbitMQOutput(QueueName = "destinationQueue", ConnectionStringSetting = "RabbitMQConnection")]
        public static string Run([RabbitMQTrigger("queue", ConnectionStringSetting = "RabbitMQConnection")] string item,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(RabbitMQFunction));

            logger.LogInformation(item);

            var message = $"Output message created at {DateTime.Now}";
            return message;
        }
    }
}
