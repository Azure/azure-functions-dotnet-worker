using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Logging
{
    internal class WorkerLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly ISystemLogWriter _systemLogWriter;
        private readonly IUserLogWriter _userLogWriter;
        private readonly IUserMetricWriter _userMetricWriter;
        private IExternalScopeProvider? _scopeProvider;

        public WorkerLoggerProvider(ISystemLogWriter systemLogWriter, IUserLogWriter userLogWriter, IUserMetricWriter userMetricWriter)
        {
            _systemLogWriter = systemLogWriter ?? throw new ArgumentNullException(nameof(systemLogWriter));
            _userLogWriter = userLogWriter ?? throw new ArgumentNullException(nameof(userLogWriter));
            _userMetricWriter = userMetricWriter ?? throw new ArgumentNullException(nameof(userMetricWriter));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new WorkerLogger(categoryName, _systemLogWriter, _userLogWriter, _userMetricWriter, _scopeProvider!);
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }
        public void Dispose()
        {
        }
    }
}
