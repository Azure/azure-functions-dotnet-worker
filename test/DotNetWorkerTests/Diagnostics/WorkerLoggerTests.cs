// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Diagnostics
{
    public class WorkerMessageTests
    {
        private readonly TestLoggerProvider _loggerProvider = new();

        [Fact]
        public void WorkerMessage1()
        {
            Action<ILogger, Exception> msg1 = WorkerMessage.Define(LogLevel.Information, new EventId(1, "One"), "No params");
            var logger = _loggerProvider.CreateLogger("1");

            msg1(logger, null);
            logger.LogInformation("info");

            var logMsg1 = _loggerProvider.GetAllLogMessages().First();
            var logMsg2 = _loggerProvider.GetAllLogMessages().Last();

            Assert.Equal("No params", logMsg1.FormattedMessage);
            Assert.True(logMsg1.IsSystemLog);
            Assert.Equal(LogLevel.Information, logMsg1.Level);
            Assert.Equal(1, logMsg1.EventId.Id);
            Assert.Equal("One", logMsg1.EventId.Name);
            Assert.Collection(logMsg1.State,
                p => Assert.True(p.Key == "{OriginalFormat}" && (string)p.Value == "No params"));

            Assert.False(logMsg2.IsSystemLog);
        }

        [Fact]
        public void WorkerMessage2()
        {
            Action<ILogger, string, Exception> msg2 = WorkerMessage.Define<string>(LogLevel.Information, new EventId(2, "Two"), "One param: {p1}");
            var logger = _loggerProvider.CreateLogger("2");

            msg2(logger, "a", null);

            var logMsg = _loggerProvider.GetAllLogMessages().Single();

            Assert.Equal("One param: a", logMsg.FormattedMessage);
            Assert.True(logMsg.IsSystemLog);
            Assert.Equal(LogLevel.Information, logMsg.Level);
            Assert.Equal(2, logMsg.EventId.Id);
            Assert.Equal("Two", logMsg.EventId.Name);
            Assert.Collection(logMsg.State,
                p => Assert.True(p.Key == "p1" && (string)p.Value == "a"),
                p => Assert.True(p.Key == "{OriginalFormat}" && (string)p.Value == "One param: {p1}"));

        }
    }
}
