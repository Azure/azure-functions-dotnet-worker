// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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

            if (File.Exists(logFilePath))
            {
                _logFilePath = logFilePath;
                return;
            }

            try
            {
                File.AppendAllText(logFilePath, $"{Environment.NewLine}Starting new session at {DateTime.Now}{Environment.NewLine}{Environment.NewLine}");
                _logFilePath = logFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating log file at {logFilePath}: {ex.Message}");
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
            Console.WriteLine(logMessage);

            if (string.IsNullOrEmpty(_logFilePath))
            {
                return;
            }

            try
            {
                File.AppendAllText(_logFilePath, $"{logMessage}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }
    }
}
