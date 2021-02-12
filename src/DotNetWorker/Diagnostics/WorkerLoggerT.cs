// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Diagnostics
{
    internal class WorkerLogger<T> : ILogger<T>
    {
        private readonly WorkerLogger _inner;
        private static Assembly _thisAssembly = typeof(WorkerLogger<>).Assembly;

        public WorkerLogger(ILoggerFactory loggerFactory)
        {
            bool isSystemAssembly = typeof(T).Assembly == _thisAssembly;
            _inner = new WorkerLogger(loggerFactory.CreateLogger(typeof(T)), isSystemAssembly);
        }

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
            _inner.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
