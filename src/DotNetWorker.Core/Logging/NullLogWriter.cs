// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Logging
{
    /// <summary>
    /// Minimalistic LogWriter that does nothing.
    /// </summary>
    internal class NullLogWriter : IUserLogWriter, ISystemLogWriter, IUserMetricWriter
    {
        private NullLogWriter()
        {
        }

        /// <summary>
        /// Returns the shared instance of <see cref="NullLogWriter"/>.
        /// </summary>
        public static NullLogWriter Instance = new NullLogWriter();

        /// <inheritdoc/>
        public void WriteSystemLog<TState>(IExternalScopeProvider scopeProvider, string categoryName, LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }

        /// <inheritdoc/>
        public void WriteUserLog<TState>(IExternalScopeProvider scopeProvider, string categoryName, LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }

        /// <inheritdoc/>
        public void WriteUserMetric(IExternalScopeProvider scopeProvider, string metricName, string metricValue, IDictionary<string, object> properties)
        {
        }

        /// <inheritdoc/>
        public void WriteUserMetric(IExternalScopeProvider scopeProvider, IDictionary<string, object> state)
        {
        }
    }
}
