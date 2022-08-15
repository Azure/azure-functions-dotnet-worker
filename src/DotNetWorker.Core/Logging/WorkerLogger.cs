using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Logging.ApplicationInsights;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Logging
{
    internal class WorkerLogger : ILogger
    {
        private readonly string _category;
        private readonly ISystemLogWriter _systemLogWriter;
        private readonly IUserLogWriter _userLogWriter;
        private readonly IUserMetricWriter _userMetricWriter;
        private readonly IExternalScopeProvider _scopeProvider;

        public WorkerLogger(string category, ISystemLogWriter systemLogWriter, IUserLogWriter userLogWriter, IUserMetricWriter userMetricWriter, IExternalScopeProvider scopeProvider)
        {
            _category = category;
            _systemLogWriter = systemLogWriter;
            _userLogWriter = userLogWriter;
            _userMetricWriter = userMetricWriter;
            _scopeProvider = scopeProvider;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            // The built-in DI wire-up guarantees that scope provider will be set.
            return _scopeProvider.Push(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (WorkerMessage.IsSystemLog)
            {
                _systemLogWriter.WriteSystemLog(_scopeProvider, _category, logLevel, eventId, state, exception, formatter);
            }
            else
            {
                if (eventId.Name == LogConstants.MetricEventId.Name)
                {
                    _userMetricWriter.WriteUserMetric(_scopeProvider, (state as IDictionary<string, object>) ?? new Dictionary<string, object>());
                    return;
                }

                _userLogWriter.WriteUserLog(_scopeProvider, _category, logLevel, eventId, state, exception, formatter);
            }
        }
    }
}
