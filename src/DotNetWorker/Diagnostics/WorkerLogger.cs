// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Diagnostics
{
    internal class WorkerLogger : ILogger
    {
        private readonly ILogger _inner;
        private readonly bool _isSystemAssembly = false;
        private static readonly AsyncLocal<bool> _isSystemLog = new AsyncLocal<bool>();

        public WorkerLogger(ILogger inner, bool isSystemAssembly = false)
        {
            _inner = inner;
            _isSystemAssembly = isSystemAssembly;
        }

        internal static bool IsSystemLog => _isSystemLog.Value;

        public IDisposable BeginScope<TState>(TState state)
        {
            return _inner.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _inner.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _isSystemLog.Value = _isSystemAssembly;
            _inner.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
