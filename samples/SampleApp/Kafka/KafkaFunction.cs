// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Kafka;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class KafkaFunction
    {
        [Function("KafkaFunction")]
        [KafkaOutput("myOutput", "LocalBroker", "stringTopicTenPartitions")]
        public static void Run([KafkaTrigger("LocalBroker", "stringTopicTenPartitions",
            ConsumerGroup = "$Default", AuthenticationMode = BrokerAuthenticationMode.Plain)] string input,
            FunctionContext context)
        {
            var logger = context.GetLogger("KafkaFunction");

            logger.LogInformation(input);

            var message = $"Output message created at {DateTime.Now}";
            context.OutputBindings["myOutput"] = message;
        }
    }
}
