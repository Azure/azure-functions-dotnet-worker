// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Timer;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class TimerFunction
    {
        [Function("TimerFunction")]
        public static void Run([TimerTrigger("0 */5 * * * *")] MyInfo timerInfo,
            FunctionContext context)
        {
            var logger = context.GetLogger("TimerFunction");
            logger.LogInformation($"Function Ran. Next timer schedule = {timerInfo.ScheduleStatus.Next}");
        }
    }

    public class MyInfo
    {
        public MyScheduleStatus ScheduleStatus { get; set; }

        public bool IsPastDue { get; set; }
    }

    public class MyScheduleStatus
    {
        public DateTime Last { get; set; }

        public DateTime Next { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
