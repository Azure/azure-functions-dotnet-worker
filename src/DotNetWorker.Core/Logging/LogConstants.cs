// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Logging.ApplicationInsights
{
    /// <summary>
    /// Keys used by the <see cref="ILogger"/> infrastructure.
    /// </summary>
    internal static class LogConstants
    {
        /// <summary>
        /// Gets the name of the key used to store the name of the function.
        /// </summary>
        internal const string NameKey = "Name";

        /// <summary>
        /// Gets the event id for a metric event.
        /// </summary>
        internal static readonly EventId MetricEventId = new EventId(1, "MS_LogMetric");

        /// <summary>
        /// Gets the name of the key used to store a metric sum.
        /// </summary>
        internal const string MetricValueKey = "Value";
    }
}
