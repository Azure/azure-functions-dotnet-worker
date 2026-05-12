// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text;

namespace FunctionsNetHost
{
    internal static class HostTraceManager
    {
        private const string DotNetHostTrace = "DOTNET_HOST_TRACE";
        private const string DotNetHostTraceVerbosity = "DOTNET_HOST_TRACE_VERBOSITY";
        private const string DotNetHostTraceFile = "DOTNET_HOST_TRACEFILE";
        private const string CoreHostTrace = "COREHOST_TRACE";
        private const string CoreHostTraceVerbosity = "COREHOST_TRACE_VERBOSITY";
        private const string CoreHostTraceFile = "COREHOST_TRACEFILE";
        private const string HostTraceVerbosity = "4";
        private const int MaxTraceBytesToEmit = 256 * 1024;
        private const int MaxTraceChunkCharacters = 8000;
        private static readonly TimeSpan TraceFlushDelay = TimeSpan.FromMinutes(1);

        internal static void ConfigureAndStartDelayedFlush()
        {
            if (!Configuration.IsHostTraceEnabled)
            {
                return;
            }

            var traceFilePath = ConfigureTraceEnvironment();
            Logger.Log($"FunctionsNetHost host tracing enabled. TraceFile:{traceFilePath}, FlushDelayMs:{TraceFlushDelay.TotalMilliseconds:0.0}, MaxTraceBytes:{MaxTraceBytesToEmit}, MaxChunkCharacters:{MaxTraceChunkCharacters}");

            ScheduleDelayedFlush("process-start");
        }

        internal static void ScheduleDelayedFlush(string reason)
        {
            if (!Configuration.IsHostTraceEnabled)
            {
                return;
            }

            var traceFilePath = GetConfiguredTraceFilePath();
            Logger.Log($".NET host trace flush scheduled. Reason:{reason}, TraceFile:{traceFilePath}, FlushDelayMs:{TraceFlushDelay.TotalMilliseconds:0.0}");

#pragma warning disable CS4014
            FlushTraceAfterDelayAsync(traceFilePath, TraceFlushDelay, Task.Delay, reason);
#pragma warning restore CS4014
        }

        internal static string ConfigureTraceEnvironment()
        {
            var traceFilePath = GetConfiguredTraceFilePath();

            SetEnvironmentValueIfEmpty(DotNetHostTrace, "1");
            SetEnvironmentValueIfEmpty(DotNetHostTraceVerbosity, HostTraceVerbosity);
            SetEnvironmentValueIfEmpty(DotNetHostTraceFile, traceFilePath);
            SetEnvironmentValueIfEmpty(CoreHostTrace, "1");
            SetEnvironmentValueIfEmpty(CoreHostTraceVerbosity, HostTraceVerbosity);
            SetEnvironmentValueIfEmpty(CoreHostTraceFile, traceFilePath);

            return traceFilePath;
        }

        internal static async Task FlushTraceAfterDelayAsync(
            string traceFilePath,
            TimeSpan delay,
            Func<TimeSpan, Task> delayAsync,
            string reason = "manual")
        {
            try
            {
                await delayAsync(delay);
                FlushTraceFile(traceFilePath, reason: reason);
            }
            catch (Exception exception)
            {
                Logger.Log(
                    $".NET host trace flush failed. Reason:{reason}, TraceFile:{traceFilePath}, ExceptionType:{exception.GetType().Name}",
                    exception.Message);
            }
        }

        internal static void FlushTraceFile(
            string traceFilePath,
            int maxTraceBytesToEmit = MaxTraceBytesToEmit,
            int maxTraceChunkCharacters = MaxTraceChunkCharacters,
            string reason = "manual")
        {
            try
            {
                if (!File.Exists(traceFilePath))
                {
                    Logger.Log($".NET host trace unavailable. FlushReason:{reason}, Reason:TraceFileMissing, TraceFile:{traceFilePath}");
                    return;
                }

                var fileInfo = new FileInfo(traceFilePath);
                var fileLength = fileInfo.Length;
                if (fileLength == 0)
                {
                    Logger.Log($".NET host trace unavailable. FlushReason:{reason}, Reason:TraceFileEmpty, TraceFile:{traceFilePath}");
                    return;
                }

                using var stream = new FileStream(traceFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var segments = ReadTraceSegments(stream, fileLength, maxTraceBytesToEmit);
                var bytesRead = segments.Sum(segment => segment.BytesRead);
                var truncated = fileLength > bytesRead;

                EmitTraceChunks(traceFilePath, fileLength, bytesRead, truncated, segments, maxTraceChunkCharacters, reason);
            }
            catch (Exception exception) when (exception is IOException
                or UnauthorizedAccessException
                or NotSupportedException
                or ArgumentException)
            {
                Logger.Log(
                    $".NET host trace unavailable. FlushReason:{reason}, Reason:TraceFileUnreadable, TraceFile:{traceFilePath}, ExceptionType:{exception.GetType().Name}",
                    exception.Message);
            }
        }

        private static string GetConfiguredTraceFilePath()
        {
            var traceFilePath = EnvironmentUtils.GetValue(DotNetHostTraceFile);
            if (!string.IsNullOrWhiteSpace(traceFilePath))
            {
                return traceFilePath;
            }

            traceFilePath = EnvironmentUtils.GetValue(CoreHostTraceFile);
            if (!string.IsNullOrWhiteSpace(traceFilePath))
            {
                return traceFilePath;
            }

            return Path.Combine(Path.GetTempPath(), $"functions-net-host-hosttrace-{Environment.ProcessId}.log");
        }

        private static void SetEnvironmentValueIfEmpty(string name, string value)
        {
            if (string.IsNullOrEmpty(EnvironmentUtils.GetValue(name)))
            {
                EnvironmentUtils.SetValue(name, value);
            }
        }

        private static void EmitTraceChunks(
            string traceFilePath,
            long fileLength,
            int bytesRead,
            bool truncated,
            IEnumerable<TraceSegment> segments,
            int maxTraceChunkCharacters,
            string reason)
        {
            var emittedAnyChunk = false;
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                {
                    continue;
                }

                var totalChunks = (int)Math.Ceiling((double)segment.Text.Length / maxTraceChunkCharacters);
                for (var chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
                {
                    var startIndex = chunkIndex * maxTraceChunkCharacters;
                    var chunkLength = Math.Min(maxTraceChunkCharacters, segment.Text.Length - startIndex);
                    var chunk = segment.Text.Substring(startIndex, chunkLength);

                    Logger.Log(
                        $".NET host trace {segment.Name} chunk {chunkIndex + 1}/{totalChunks}. FlushReason:{reason}, TraceFile:{traceFilePath}, FileBytes:{fileLength}, EmittedBytes:{bytesRead}, SegmentOffset:{segment.Offset}, SegmentBytes:{segment.BytesRead}, Truncated:{truncated}",
                        chunk);
                    emittedAnyChunk = true;
                }
            }

            if (!emittedAnyChunk)
            {
                Logger.Log($".NET host trace unavailable. FlushReason:{reason}, Reason:TraceFileEmptyAfterRead, TraceFile:{traceFilePath}, FileBytes:{fileLength}");
            }
        }

        private static IReadOnlyList<TraceSegment> ReadTraceSegments(FileStream stream, long fileLength, int maxTraceBytesToEmit)
        {
            if (fileLength <= maxTraceBytesToEmit)
            {
                return [ReadTraceSegment(stream, "full", 0, (int)fileLength)];
            }

            var startBytesToRead = maxTraceBytesToEmit / 2;
            var endBytesToRead = maxTraceBytesToEmit - startBytesToRead;
            var endOffset = fileLength - endBytesToRead;

            return
            [
                ReadTraceSegment(stream, "start", 0, startBytesToRead),
                ReadTraceSegment(stream, "end", endOffset, endBytesToRead),
            ];
        }

        private static TraceSegment ReadTraceSegment(FileStream stream, string name, long offset, int bytesToRead)
        {
            var buffer = new byte[bytesToRead];
            stream.Seek(offset, SeekOrigin.Begin);
            var bytesRead = stream.Read(buffer, 0, bytesToRead);

            return new TraceSegment(name, offset, bytesRead, Encoding.UTF8.GetString(buffer, 0, bytesRead));
        }

        private sealed record TraceSegment(string Name, long Offset, int BytesRead, string Text);
    }
}
