// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker
{
    internal static class WorkerMessage
    {
        private static readonly AsyncLocal<bool> _isSystemLog = new AsyncLocal<bool>();

        internal static bool IsSystemLog => _isSystemLog.Value;

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message. This message is treated as a "system" log
        /// and forwarded to the Functions host in addition to other loggers.
        /// </summary>
        public static Action<ILogger, Exception?> Define(LogLevel logLevel, EventId eventId, string formatString)
        {
            var log = LoggerMessage.Define(logLevel, eventId, formatString);

            return (logger, exception) =>
            {
                try
                {
                    _isSystemLog.Value = true;
                    log(logger, exception);
                }
                finally
                {
                    _isSystemLog.Value = false;
                }
            };
        }
        /// <summary>
        /// Creates a delegate which can be invoked for logging a message. This message is treated as a "system" log
        /// and forwarded to the Functions host in addition to other loggers.
        /// </summary>
        public static Action<ILogger, T1, Exception?> Define<T1>(LogLevel logLevel, EventId eventId, string formatString)
        {
            var log = LoggerMessage.Define<T1>(logLevel, eventId, formatString);

            return (logger, arg1, exception) =>
            {
                try
                {
                    _isSystemLog.Value = true;
                    log(logger, arg1, exception);
                }
                finally
                {
                    _isSystemLog.Value = false;
                }
            };
        }

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message. This message is treated as a "system" log
        /// and forwarded to the Functions host in addition to other loggers.
        /// </summary>
        public static Action<ILogger, T1, T2, Exception?> Define<T1, T2>(LogLevel logLevel, EventId eventId, string formatString)
        {
            var log = LoggerMessage.Define<T1, T2>(logLevel, eventId, formatString);

            return (logger, arg1, arg2, exception) =>
            {
                try
                {
                    _isSystemLog.Value = true;
                    log(logger, arg1, arg2, exception);
                }
                finally
                {
                    _isSystemLog.Value = false;
                }
            };
        }
    }
}
