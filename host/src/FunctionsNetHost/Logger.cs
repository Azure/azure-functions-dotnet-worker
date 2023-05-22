// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Globalization;

namespace FunctionsNetHost
{
    internal static class Logger
    {
        private static readonly bool IsDebugLevelLogEnabled;
        private static readonly string LogPrefix;

        static Logger()
        {
#if !DEBUG
            LogPrefix = "LanguageWorkerConsoleLog";
#else
            LogPrefix = "";
#endif
            string traceLoggingEnabled =
                Environment.GetEnvironmentVariable(EnvironmentSettingNames.FunctionsNetHostTrace) ?? "0";
            IsDebugLevelLogEnabled = traceLoggingEnabled == "1";
        }

        internal static bool IsDebugLogEnabled => IsDebugLevelLogEnabled;

        /// <summary>
        /// Logs a debug message if "AZURE_FUNCTIONS_FUNCTIONSNETHOST_TRACE" environment variable value is set to "1"
        /// For optimal performance (when building the log message needs execute some code (Ex: someObj.ToString() or so),
        /// consider checking "IsDebugLogEnabled" before calling LogDebug.
        /// </summary>
        internal static void LogDebug(string message)
        {
            if (IsDebugLevelLogEnabled)
            {
                Log(message);
            }
        }

        /// <summary>
        /// Logs a debug message if "AZURE_FUNCTIONS_FUNCTIONSNETHOST_TRACE" environment variable value is set to "1"
        /// </summary>
        public static void LogDebug(Func<string> messageProvider)
        {
            if (IsDebugLevelLogEnabled)
            {
                string message = messageProvider();
                Log(message);
            }
        }

        internal static void LogInfo(string message) => Log(message);

        private static void Log(string message)
        {
            string ts = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            Console.WriteLine($"{LogPrefix}[{ts}] [FunctionsNetHost] {message}");
        }
    }
}
