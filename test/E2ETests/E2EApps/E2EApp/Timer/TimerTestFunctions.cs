// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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
    }
}
