// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Xunit;

namespace FunctionsNetHost.Tests
{
    public class HostTraceManagerTests
    {
        [Fact]
        public void ConfigureTraceEnvironment_SetsHostTraceEnvironmentVariables()
        {
            using var dotNetTrace = new EnvironmentVariableScope("DOTNET_HOST_TRACE", null);
            using var dotNetTraceVerbosity = new EnvironmentVariableScope("DOTNET_HOST_TRACE_VERBOSITY", null);
            using var dotNetTraceFile = new EnvironmentVariableScope("DOTNET_HOST_TRACEFILE", null);
            using var coreHostTrace = new EnvironmentVariableScope("COREHOST_TRACE", null);
            using var coreHostTraceVerbosity = new EnvironmentVariableScope("COREHOST_TRACE_VERBOSITY", null);
            using var coreHostTraceFile = new EnvironmentVariableScope("COREHOST_TRACEFILE", null);

            var traceFilePath = HostTraceManager.ConfigureTraceEnvironment();

            Assert.Equal("1", Environment.GetEnvironmentVariable("DOTNET_HOST_TRACE"));
            Assert.Equal("4", Environment.GetEnvironmentVariable("DOTNET_HOST_TRACE_VERBOSITY"));
            Assert.Equal(traceFilePath, Environment.GetEnvironmentVariable("DOTNET_HOST_TRACEFILE"));
            Assert.Equal("1", Environment.GetEnvironmentVariable("COREHOST_TRACE"));
            Assert.Equal("4", Environment.GetEnvironmentVariable("COREHOST_TRACE_VERBOSITY"));
            Assert.Equal(traceFilePath, Environment.GetEnvironmentVariable("COREHOST_TRACEFILE"));
            Assert.Contains($"functions-net-host-hosttrace-{Environment.ProcessId}.log", traceFilePath, StringComparison.Ordinal);
        }

        [Fact]
        public void FlushTraceFile_EmitsStartAndEndChunksWhenTraceIsTruncated()
        {
            var traceFilePath = Path.GetTempFileName();
            File.WriteAllText(traceFilePath, "0123456789abcdefghijKLMNOPQRST");
            var logs = new List<string>();

            using var writer = Logger.ReplaceWriterForTests(logs.Add);
            try
            {
                HostTraceManager.FlushTraceFile(traceFilePath, maxTraceBytesToEmit: 20, maxTraceChunkCharacters: 10);
            }
            finally
            {
                File.Delete(traceFilePath);
            }

            Assert.Equal(2, logs.Count);
            Assert.Contains(".NET host trace start chunk 1/1", logs[0], StringComparison.Ordinal);
            Assert.Contains("EmittedBytes:20", logs[0], StringComparison.Ordinal);
            Assert.Contains("SegmentOffset:0", logs[0], StringComparison.Ordinal);
            Assert.Contains("SegmentBytes:10", logs[0], StringComparison.Ordinal);
            Assert.Contains("Truncated:True", logs[0], StringComparison.Ordinal);
            Assert.Contains("\"0123456789\"", logs[0], StringComparison.Ordinal);
            Assert.Contains(".NET host trace end chunk 1/1", logs[1], StringComparison.Ordinal);
            Assert.Contains("SegmentOffset:20", logs[1], StringComparison.Ordinal);
            Assert.Contains("SegmentBytes:10", logs[1], StringComparison.Ordinal);
            Assert.Contains("\"KLMNOPQRST\"", logs[1], StringComparison.Ordinal);
            Assert.DoesNotContain("\"abcdefghij\"", logs[1], StringComparison.Ordinal);
        }

        [Fact]
        public void FlushTraceFile_EmitsFullChunksWhenTraceIsWithinLimit()
        {
            var traceFilePath = Path.GetTempFileName();
            File.WriteAllText(traceFilePath, "0123456789abcdefghij");
            var logs = new List<string>();

            using var writer = Logger.ReplaceWriterForTests(logs.Add);
            try
            {
                HostTraceManager.FlushTraceFile(traceFilePath, maxTraceBytesToEmit: 20, maxTraceChunkCharacters: 10);
            }
            finally
            {
                File.Delete(traceFilePath);
            }

            Assert.Equal(2, logs.Count);
            Assert.Contains(".NET host trace full chunk 1/2", logs[0], StringComparison.Ordinal);
            Assert.Contains("EmittedBytes:20", logs[0], StringComparison.Ordinal);
            Assert.Contains("SegmentOffset:0", logs[0], StringComparison.Ordinal);
            Assert.Contains("SegmentBytes:20", logs[0], StringComparison.Ordinal);
            Assert.Contains("Truncated:False", logs[0], StringComparison.Ordinal);
            Assert.Contains("\"0123456789\"", logs[0], StringComparison.Ordinal);
            Assert.Contains(".NET host trace full chunk 2/2", logs[1], StringComparison.Ordinal);
            Assert.Contains("\"abcdefghij\"", logs[1], StringComparison.Ordinal);
        }

        [Fact]
        public void FlushTraceFile_LogsWhenTraceFileIsMissing()
        {
            var logs = new List<string>();
            using var writer = Logger.ReplaceWriterForTests(logs.Add);

            HostTraceManager.FlushTraceFile(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

            var log = Assert.Single(logs);
            Assert.Contains(".NET host trace unavailable", log, StringComparison.Ordinal);
            Assert.Contains("Reason:TraceFileMissing", log, StringComparison.Ordinal);
        }
    }
}
