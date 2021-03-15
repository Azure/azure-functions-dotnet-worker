// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Represents a timer schedule status.
    /// </summary>
    public class ScheduleStatus
    {
        /// <summary>
        /// Gets or sets the last recorded schedule occurrence.
        /// </summary>
        public DateTime Last { get; set; }

        /// <summary>
        /// Gets or sets the expected next schedule occurrence.
        /// </summary>
        public DateTime Next { get; set; }

        /// <summary>
        /// Gets or sets the last time this record was updated. This is used to re-calculate Next
        /// with the current Schedule after a host restart.
        /// </summary>
        public DateTime LastUpdated { get; set; }
    }
}
