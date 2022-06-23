// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Logging
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
        internal readonly static EventId MetricEventId = new EventId(1, "MS_LogMetric");

        /// <summary>
        /// Gets the name of the key used to store a metric sum.
        /// </summary>
        internal const string MetricValueKey = "Value";

        /// <summary>
        /// Gets the name of the key used to store the function invocation id.
        /// </summary>
        public const string InvocationIdKey = "InvocationId";

        /// <summary>
        /// Gets the name of the key used to store the category of the log message.
        /// </summary>
        public const string CategoryNameKey = "Category";

        /// <summary>
        ///  Get the name of the key to store the current process id.
        /// </summary>
        public const string ProcessIdKey = "ProcessId";
    }
}
