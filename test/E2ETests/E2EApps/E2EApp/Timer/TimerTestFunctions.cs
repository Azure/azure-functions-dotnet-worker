// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp.Queue
{
    public class TimerTestFunctions
    {
        private readonly ILogger<TimerTestFunctions> _logger;

        public TimerTestFunctions(ILogger<TimerTestFunctions> logger)
        {
            _logger = logger;
        }

        [Function(nameof(TimerTrigger))]
        public void TimerTrigger([TimerTrigger("0 * * * * *", RunOnStartup = true)] TimerInfo timerInfo)
        {
            var info = JsonSerializer.Serialize(timerInfo);
            _logger.LogInformation($"TimerInfo: {info}");
        }

        [Function(nameof(TimerTrigger1))]
        [WebPubSubOutput(Hub = "notification")]
        public WebPubSubAction TimerTrigger1([TimerTrigger("*/10 * * * * *", RunOnStartup = true)] TimerInfo timerInfo)
        {
            var info = JsonSerializer.Serialize(timerInfo);
            _logger.LogInformation($"TimerInfo: {info}");
            return new SendToAllAction
            {
                Data = BinaryData.FromString($"[Isolate][DateTime: {DateTime.Now}] Temperature: 22.2{'\xB0'}C, Humidity: 44.4%"),
                DataType = WebPubSubDataType.Text
            };
        }
    }
}
