// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class TestLogger : ILogger
    {
        private readonly object _syncLock = new object();
        private readonly IExternalScopeProvider _scopeProvider;
        private IList<LogMessage> _logMessages = new List<LogMessage>();

        public TestLogger(string category)
            : this(category, new LoggerExternalScopeProvider())
        {
        }

        public TestLogger(string category, IExternalScopeProvider scopeProvider)
        {
            Category = category;
            _scopeProvider = scopeProvider;
        }

        public string Category { get; private set; }

        private string DebuggerDisplay => $"Category: {Category}, Count: {_logMessages.Count}";

        public IDisposable BeginScope<TState>(TState state) => _scopeProvider.Push(state);

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IList<LogMessage> GetLogMessages()
        {
            lock (_syncLock)
            {
                return _logMessages.ToList();
            }
        }

        public void ClearLogMessages()
        {
            lock (_syncLock)
            {
                _logMessages.Clear();
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            LogMessage logMessage = new LogMessage
            {
                Level = logLevel,
                EventId = eventId,
                State = state as IEnumerable<KeyValuePair<string, object>>,
                Scope = GetScopeDictionaryOrNull(_scopeProvider),
                IsSystemLog = WorkerMessage.IsSystemLog,
                Exception = exception,
                FormattedMessage = formatter(state, exception),
                Category = Category,
                Timestamp = DateTime.UtcNow
            };

            lock (_syncLock)
            {
                _logMessages.Add(logMessage);
            }
        }

        private static IDictionary<string, object> GetScopeDictionaryOrNull(IExternalScopeProvider scopeProvider)
        {
            IDictionary<string, object> result = null;

            scopeProvider?.ForEachScope((scope, _) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object>> kvps)
                {
                    result = result ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                    foreach (var kvp in kvps)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }
            }, (object)null);

            return result;
        }
    }
}
