// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Globalization;

namespace FunctionsNetHost
{
    internal static class Logger
    {
        private static readonly string LogPrefix;
        private static readonly bool IsTraceLogEnabled;
        private static string? _logFilePath;

        static Logger()
        {
            IsTraceLogEnabled = string.Equals(EnvironmentUtils.GetValue(EnvironmentVariables.EnableTraceLogs), "1");
            var disableLogPrefix = string.Equals(EnvironmentUtils.GetValue(EnvironmentVariables.DisableLogPrefix), "1");
            LogPrefix = disableLogPrefix ? string.Empty : "LanguageWorkerConsoleLog";

            CreateLogFile();
        }

        private static void CreateLogFile()
        {
            var logFilePath = EnvironmentUtils.GetValue(EnvironmentVariables.LogFilePath);

            if (logFilePath == null)
            {
                return;
            }
            var pid = Process.GetCurrentProcess().Id;
            var fileExist = File.Exists(logFilePath);
            if (!fileExist)
            {
                try
                {
                    File.AppendAllText(logFilePath, $"{Environment.NewLine}Log file created at {DateTime.UtcNow}(UTC){Environment.NewLine}PID:{pid}{Environment.NewLine}");
                    fileExist = true;
                    _logFilePath = logFilePath;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating log file at {logFilePath}: {ex.Message}");
                }
                return;
            }
            else
            {
                _logFilePath = logFilePath;
                File.AppendAllText(_logFilePath, $"{Environment.NewLine}{Environment.NewLine}PID:{pid}{Environment.NewLine}");
            }
        }

        /// <summary>
        /// Logs a trace message if "AZURE_FUNCTIONS_FUNCTIONSNETHOST_TRACE" environment variable value is set to "1"
        /// </summary>
        internal static void LogTrace(string message)
        {
            if (IsTraceLogEnabled)
            {
                Log(message);
            }
        }

        internal static void Log(string message)
        {
            var ts = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var logMessage = $"{LogPrefix}[{ts}] [FunctionsNetHost] {message}";

            if (!string.IsNullOrEmpty(_logFilePath))
            {
                try
                {
                    File.AppendAllText(_logFilePath, $"{logMessage}{Environment.NewLine}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to log file: {ex.Message}");
                }
            }
            else
            {
                // When file logging is enabled, don't write to stdout
                Console.WriteLine(logMessage);
            }
        }
    }
}
