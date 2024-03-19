// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Infrastructure
{
    internal sealed partial class ExtensionTrace : ILogger
    {
        private readonly ILogger _defaultLogger;

        public ExtensionTrace(ILoggerFactory loggerFactory)
        {
            _defaultLogger = loggerFactory.CreateLogger("Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore");
        }

        public IDisposable BeginScope<TState>(TState state) => _defaultLogger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _defaultLogger.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => _defaultLogger.Log(logLevel, eventId, state, exception, formatter);
    }
}
