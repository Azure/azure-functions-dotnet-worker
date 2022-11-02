// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class TestLogger<T> : TestLogger, ILogger<T>
    {
        private TestLogger(string category)
            : base(category)
        {
        }

        public static TestLogger<T> Create()
        {
            // We want to use the logic for category naming which is internal to LoggerFactory.
            // So we'll create a TestLogger via the LoggerFactory and grab it's category.
            TestLoggerProvider testLoggerProvider = new TestLoggerProvider();
            LoggerFactory _testLoggerFactory = new LoggerFactory(new[] { testLoggerProvider });
            _testLoggerFactory.CreateLogger<T>();
            TestLogger testLogger = testLoggerProvider.CreatedLoggers.Single();
            return new TestLogger<T>(testLogger.Category);
        }
    }
}
