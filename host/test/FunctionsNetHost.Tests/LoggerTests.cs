// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Xunit;

namespace FunctionsNetHost.Tests
{
    public class LoggerTests
    {
        [Fact]
        public void Log_WritesMsFunctionLogsLine()
        {
            var logs = new List<string>();
            using var writer = Logger.ReplaceWriterForTests(logs.Add);
            using var containerName = new EnvironmentVariableScope(EnvironmentVariables.ContainerName, "container-name");
            using var stampName = new EnvironmentVariableScope(EnvironmentVariables.StampName, "WestUS");
            using var tenantId = new EnvironmentVariableScope(EnvironmentVariables.TenantId, "Tenant-Id");

            Logger.Log("summary \"value\"\r\nnext line", "details\r\nnext line");

            var log = Assert.Single(logs);
            Assert.StartsWith("MS_FUNCTION_LOGS ", log, StringComparison.Ordinal);
            Assert.Contains(",FunctionsNetHost,", log, StringComparison.Ordinal);
            Assert.Contains("\"details next line\"", log, StringComparison.Ordinal);
            Assert.Contains("\"summary 'value' next line\"", log, StringComparison.Ordinal);
            Assert.Contains(",CONTAINER-NAME,westus,tenant-id,", log, StringComparison.Ordinal);
        }
    }
}
