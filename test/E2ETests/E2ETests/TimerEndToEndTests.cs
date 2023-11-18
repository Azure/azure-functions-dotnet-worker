// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Tests.E2ETests
{
    [Collection(Constants.FunctionAppCollectionName)]
    public class TimerEndToEndTests : IDisposable
    {
        private readonly IDisposable _disposeLog;
        private readonly FunctionAppFixture _fixture;

        public TimerEndToEndTests(FunctionAppFixture fixture, ITestOutputHelper testOutput)
        {
            _fixture = fixture;
            _disposeLog = _fixture.TestLogs.UseTestLogger(testOutput);
        }

        [Fact]
        public async Task TimerTriggerWithTimerInfo_Succeeds()
        {
            string key = "TimerInfo: ";

            IEnumerable<string> logs = null;
            await TestUtility.RetryAsync(() =>
            {
                logs = _fixture.TestLogs.CoreToolsLogs.Where(p => p.Contains(key));
                // The "RunOnStartup" log should show, and then a true invocation.
                return Task.FromResult(logs.Count() >= 2);
            });

            // Check the serialized TimerInfo; they should all be valid values.
            var lastLog = logs.Last();
            int subStringStart = lastLog.LastIndexOf(key) + key.Length;
            var doc = JsonDocument.Parse(lastLog[subStringStart..]);

            Assert.NotEqual(DateTimeOffset.MinValue, doc.RootElement.GetProperty("ScheduleStatus").GetProperty("Last").GetDateTimeOffset());
            Assert.NotEqual(DateTimeOffset.MinValue, doc.RootElement.GetProperty("ScheduleStatus").GetProperty("Next").GetDateTimeOffset());
            Assert.NotEqual(DateTimeOffset.MinValue, doc.RootElement.GetProperty("ScheduleStatus").GetProperty("LastUpdated").GetDateTimeOffset());

            // This will throw if it's not there or is not a bool.
            doc.RootElement.GetProperty("IsPastDue").GetBoolean();
        }

        public void Dispose()
        {
            _disposeLog?.Dispose();
        }
    }
}
