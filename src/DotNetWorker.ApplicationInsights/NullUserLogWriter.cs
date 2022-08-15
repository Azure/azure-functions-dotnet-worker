using System;
using Microsoft.Azure.Functions.Worker.Logging;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights
{
    internal class NullUserLogWriter : IUserLogWriter
    {
        private NullUserLogWriter()
        {
        }

        public static NullUserLogWriter Instance = new NullUserLogWriter();

        public void WriteUserLog<TState>(IExternalScopeProvider scopeProvider, string categoryName, LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }
    }
}
