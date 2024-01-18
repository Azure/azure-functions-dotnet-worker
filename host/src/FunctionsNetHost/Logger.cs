// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Globalization;

namespace FunctionsNetHost
{
    internal static class Logger
    {
        private static readonly string LogPrefix;

        static Logger()
        {
            var disableLogPrefix = string.Equals(EnvironmentUtils.GetValue(EnvironmentVariables.FunctionsNetHostDisableLogPrefix), "1");
            LogPrefix = disableLogPrefix ? string.Empty : "LanguageWorkerConsoleLog";
        }

        internal static bool IsTraceLogEnabled
        {
            get
            {
                return string.Equals(EnvironmentUtils.GetValue(EnvironmentVariables.FunctionsNetHostTrace), "1");
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
            Console.WriteLine($"{LogPrefix}[{ts}] [FunctionsNetHost] {message}");
        }
    }
}
