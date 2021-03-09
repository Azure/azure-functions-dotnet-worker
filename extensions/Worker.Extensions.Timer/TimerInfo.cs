// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Provides access to timer schedule information for jobs triggered 
    /// by <see cref="TimerTriggerAttribute"/>.
    /// </summary>
    public class TimerInfo
    {
        /// <summary>
        /// Gets the current schedule status for this timer.
        /// If schedule monitoring is not enabled for this timer (see <see cref="TimerTriggerAttribute.UseMonitor"/>)
        /// this property will return null.
        /// </summary>
        public ScheduleStatus? ScheduleStatus { get; set; }

        /// <summary>
        /// Gets a value indicating whether this timer invocation
        /// is due to a missed schedule occurrence.
        /// </summary>
        public bool IsPastDue { get; set; }
    }
}
