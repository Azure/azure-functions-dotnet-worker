// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Diagnostics
{
    public class WorkerLoggerTests
    {
        private readonly TestLoggerProvider _loggerProvider = new();

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WorkerLogger_IsSystemLog(bool isSystemLog)
        {
            WorkerLogger logger = new(_loggerProvider.CreateLogger("TestLogger"), isSystemAssembly: isSystemLog);

            logger.LogInformation("Test");

            Assert.Equal(isSystemLog, _loggerProvider.GetAllLogMessages().Single().IsSystemLog);
        }

        [Fact]
        public void WorkerOfT_SetsSystemAssembly_True()
        {
            var logger = CreateWorkerLogger<HttpRequestData>();

            logger.LogInformation("Test");

            Assert.True(_loggerProvider.GetAllLogMessages().Single().IsSystemLog);
        }

        [Fact]
        public void WorkerOfT_SetsSystemAssembly_False()
        {
            var logger = CreateWorkerLogger<WorkerLoggerTests>();

            logger.LogInformation("Test");

            Assert.False(_loggerProvider.GetAllLogMessages().Single().IsSystemLog);
        }

        private ILogger CreateWorkerLogger<T>()
        {
            return new ServiceCollection()
                .AddLogging(logging =>
                {
                    logging.AddProvider(_loggerProvider);
                    logging.Services.AddSingleton(typeof(ILogger<>), typeof(WorkerLogger<>));
                })
                .BuildServiceProvider()
                .GetService<ILogger<T>>();
        }
    }
}
