// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class KafkaFunction
    {
        [Function(nameof(KafkaFunction))]
        [FixedDelayRetry(5, "00:00:10")]
        [KafkaOutput("LocalBroker", "stringTopicTenPartitions")]
        public static string Run([KafkaTrigger("LocalBroker", "stringTopicTenPartitions",
            ConsumerGroup = "$Default", AuthenticationMode = BrokerAuthenticationMode.Plain)] string input,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(KafkaFunction));

            logger.LogInformation(input);

            var message = $"Output message created at {DateTime.Now}";
            return message;
        }
    }
}
