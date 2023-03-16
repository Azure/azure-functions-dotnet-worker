// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class FunctionsApplicationTests
    {
        [Fact]
        public async Task InvokeAsync_LogsException()
        {
            static Task InvokeWithError(FunctionContext context)
            {
                throw new InvalidOperationException("boom!");
            }

            var options = new OptionsWrapper<WorkerOptions>(new WorkerOptions());
            var contextFactory = new Mock<IFunctionContextFactory>();
            var logger = TestLogger<FunctionsApplication>.Create();
            var diagnostics = new Mock<IWorkerDiagnostics>();
            var activityFactory = new FunctionActivitySourceFactory(new OptionsWrapper<WorkerOptions>(new WorkerOptions()));

            var app = new FunctionsApplication(InvokeWithError, contextFactory.Object, options, logger, diagnostics.Object, activityFactory);

            var context = new TestFunctionContext();

            await Assert.ThrowsAsync<InvalidOperationException>(() => app.InvokeFunctionAsync(context));

            var message = logger.GetLogMessages().Single();

            var name = message.State.Single(p => p.Key == "functionName").Value;
            Assert.Equal(context.FunctionDefinition.Name, name.ToString());

            var invocation = message.State.Single(p => p.Key == "invocationId").Value;
            Assert.Equal(context.InvocationId, invocation.ToString());

            Assert.Equal(LogLevel.Error, message.Level);
            Assert.IsType<InvalidOperationException>(message.Exception);
            Assert.Equal("boom!", message.Exception.Message);
            Assert.Equal("InvocationError", message.EventId.Name);
            Assert.Equal(2, message.EventId.Id);
        }
    }
}
