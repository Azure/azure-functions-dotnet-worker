// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Logging
{
    /// <summary>
    /// An abstraction for writing system logs.
    /// </summary>
    internal interface ISystemLogWriter
    {
        /// <summary>
        /// Writes a system log entry.
        /// </summary>
        /// <typeparam name="TState">The type of the object to be written.</typeparam>
        /// <param name="scopeProvider">The provider of scope data.</param>
        /// <param name="categoryName">The category name for messages produced by the logger.</param>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">Id of the event.</param>
        /// <param name="state">The entry to be written. Can be also an object.</param>
        /// <param name="exception">The exception related to this entry.</param>
        /// <param name="formatter"> Function to create a <see cref="System.String"/> message of the state and exception.</param>
        void WriteSystemLog<TState>(IExternalScopeProvider scopeProvider, string categoryName, LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter);
    }
}
