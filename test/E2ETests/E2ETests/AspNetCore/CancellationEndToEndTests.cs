﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Tests;
using Microsoft.Azure.Functions.Tests.E2ETests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Worker.E2ETests.AspNetCore
{
    public class CancellationEndToEndTests : IClassFixture<CancellationEndToEndTests.TestFixture>
    {
        private readonly TestFixture _fixture;

        public CancellationEndToEndTests(TestFixture fixture, ITestOutputHelper testOutputHelper)
        {
            _fixture = fixture;
            _fixture.TestLogs.UseTestLogger(testOutputHelper);
        }

        [IgnoreOnNetFxTestRunTheory]
        [InlineData("HttpWithCancellationTokenNotUsed", "Work completed.", "Succeeded")]
        [InlineData("HttpWithCancellationTokenIgnored", "TaskCanceledException: A task was canceled", "Failed")]
        [InlineData("HttpWithCancellationTokenHandled", "Request was cancelled.", "Succeeded")]
        public async Task HttpTriggerFunctions_WithCancellationToken_BehaveAsExpected(string functionName, string expectedMessage, string invocationResult)
        {
            Console.WriteLine($"Running test for function: {functionName}");
            using var cts = new CancellationTokenSource();

            var task = HttpHelpers.InvokeHttpTrigger(functionName, cancellationToken: cts.Token);

            await Task.Delay(2000);
            cts.Cancel();
            Console.WriteLine("Task cancelled");

            // The task should be cancelled before it completes, mimicing a client closing the connection.
            // This should lead to the worker getting an InvocationCancel request from the functions host
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);

            IEnumerable<string> logs = null;
            await TestUtility.RetryAsync(() =>
            {
                Console.WriteLine("Waiting function execution to be completed");
                logs = _fixture.TestLogs.CoreToolsLogs.Where(p => p.Contains($"Executed 'Functions.{functionName}'"));

                return Task.FromResult(logs.Count() >= 1);
            });

            Console.WriteLine("Function execution completed");

            Assert.Contains(_fixture.TestLogs.CoreToolsLogs, log => log.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase));

            // TODO: 2/3 of the test invocations will fail until the host with the ForwarderProxy fix is released - uncomment this line when the fix is released
            // Assert.Contains(_fixture.TestLogs.CoreToolsLogs, log => log.Contains($"'Functions.{functionName}' ({invocationResult}", StringComparison.OrdinalIgnoreCase));
        }

        public class TestFixture : FunctionAppFixture
        {
            public TestFixture(IMessageSink messageSink) : base(messageSink, Constants.TestAppNames.E2EAspNetCoreApp)
            {
            }
        }
    }
}
