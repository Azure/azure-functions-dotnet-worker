// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class TimerFunction
    {
        //<docsnippet_fixed_delay_retry_example>
        [Function(nameof(TimerFunction))]
        [FixedDelayRetry(5, "00:00:10")]
        public static void Run([TimerTrigger("0 */5 * * * *")] TimerInfo timerInfo,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(TimerFunction));
            logger.LogInformation($"Function Ran. Next timer schedule = {timerInfo.ScheduleStatus.Next}");
        }
        //</docsnippet_fixed_delay_retry_example>
    }
}
