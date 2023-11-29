// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Azure.Functions.Worker.E2ETests.Helpers
{
    internal class TestLoggerProvider : ILoggerProvider, ILogger
    {
        private readonly IMessageSink _messageSink;
        private ITestOutputHelper _currentTestOutput;
        ConcurrentQueue<string> _logs = new ConcurrentQueue<string>();

        public TestLoggerProvider(IMessageSink messageSink)
        {
            _messageSink = messageSink;
        }

        public IEnumerable<string> CoreToolsLogs => _logs.ToArray();

        // This needs to be created/disposed per-test so we can associate logs
        // with the specific running test.
        public IDisposable UseTestLogger(ITestOutputHelper testOutput)
        {
            // reset these every test
            _currentTestOutput = testOutput;
            return new DisposableOutput(this);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return this;
        }

        public void Dispose()
        {
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string formattedString = formatter(state, exception);
            _messageSink.OnMessage(new DiagnosticMessage(formattedString));
            _logs.Enqueue(formattedString);
            _currentTestOutput?.WriteLine(formattedString);
        }

        public void method(string loglines)
        {
            _currentTestOutput?.WriteLine(loglines);
        }

        private class DisposableOutput : IDisposable
        {
            private readonly TestLoggerProvider _xunitLogger;

            public DisposableOutput(TestLoggerProvider xunitLogger)
            {
                _xunitLogger = xunitLogger;
            }

            public void Dispose()
            {
                _xunitLogger._currentTestOutput = null;
            }
        }
    }
}
