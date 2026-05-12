// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Globalization;

namespace FunctionsNetHost
{
    internal static class Logger
    {
        private const string EventStreamName = "MS_FUNCTION_LOGS";
        private const string EventTimestampFormat = "O";
        private const string EmptyQuotedField = "\"\"";
        private const string Source = "FunctionsNetHost";
        private static readonly string ProcessId = Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
        private static readonly long ProcessStartTimestamp = Stopwatch.GetTimestamp();
        private static readonly object WriterLock = new();
        private static Action<string> _writeLine = WriteConsoleLine;

        /// <summary>
        /// Logs a trace message if trace level logging is enabled.
        /// </summary>
        internal static void LogTrace(string message)
        {
            if (Configuration.IsTraceLogEnabled)
            {
                Log(message);
            }
        }

        internal static void Log(string message)
        {
            Log(message, details: null);
        }

        internal static void Log(string message, string? details)
        {
            string[] fields =
            [
                "4",                                                                 // Level
                string.Empty,                                                        // SubscriptionId
                string.Empty,                                                        // AppName
                string.Empty,                                                        // FunctionName
                string.Empty,                                                        // EventName
                Source,                                                              // Source
                NormalizeString(details),                                            // Details
                NormalizeString(message),                                            // Summary
                string.Empty,                                                        // HostVersion
                DateTime.UtcNow.ToString(EventTimestampFormat, CultureInfo.InvariantCulture), // EventTimestamp
                string.Empty,                                                        // ExceptionType
                EmptyQuotedField,                                                    // ExceptionMessage
                string.Empty,                                                        // FunctionInvocationId
                string.Empty,                                                        // HostInstanceId
                string.Empty,                                                        // ActivityId
                NormalizeContainerName(EnvironmentUtils.GetValue(EnvironmentVariables.ContainerName)),
                NormalizeStampName(EnvironmentUtils.GetValue(EnvironmentVariables.StampName)),
                NormalizeTenantId(EnvironmentUtils.GetValue(EnvironmentVariables.TenantId)),
                string.Empty,                                                        // RuntimeSiteName
                string.Empty,                                                        // SlotName
                ProcessId                                                            // Pid
            ];

            WriteLine($"{EventStreamName} {string.Join(",", fields)}");
        }

        internal static TimeSpan GetElapsedSinceProcessStart()
        {
            return Stopwatch.GetElapsedTime(ProcessStartTimestamp);
        }

        internal static IDisposable ReplaceWriterForTests(Action<string> writeLine)
        {
            ArgumentNullException.ThrowIfNull(writeLine);

            lock (WriterLock)
            {
                var originalWriteLine = _writeLine;
                _writeLine = writeLine;

                return new DisposableAction(() =>
                {
                    lock (WriterLock)
                    {
                        _writeLine = originalWriteLine;
                    }
                });
            }
        }

        internal static string NormalizeString(string? value)
        {
            string normalized = value ?? string.Empty;
            normalized = normalized.Replace("\r\n", " ", StringComparison.Ordinal);
            normalized = normalized.Replace("\r", " ", StringComparison.Ordinal);
            normalized = normalized.Replace("\n", " ", StringComparison.Ordinal);
            normalized = normalized.Replace("\"", "'", StringComparison.Ordinal);

            return $"\"{normalized}\"";
        }

        private static string NormalizeContainerName(string? containerName)
            => containerName?.ToUpperInvariant() ?? string.Empty;

        private static string NormalizeStampName(string? stampName)
            => stampName?.ToLowerInvariant() ?? string.Empty;

        private static string NormalizeTenantId(string? tenantId)
            => tenantId?.ToLowerInvariant() ?? string.Empty;

        private static void WriteLine(string line)
        {
            try
            {
                Action<string> writeLine;
                lock (WriterLock)
                {
                    writeLine = _writeLine;
                }

                writeLine(line);
            }
            catch
            {
                // Logging is best-effort and should not take down FunctionsNetHost.
            }
        }

        private static void WriteConsoleLine(string line)
        {
            Console.Out.WriteLine(line);
            Console.Out.Flush();
        }

        private sealed class DisposableAction(Action dispose) : IDisposable
        {
            public void Dispose()
            {
                dispose();
            }
        }
    }
}
