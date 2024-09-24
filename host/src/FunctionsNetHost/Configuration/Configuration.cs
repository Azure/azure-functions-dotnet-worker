// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace FunctionsNetHost
{
    /// <summary>
    /// Represents the application configuration.
    /// </summary>
    internal static class Configuration
    {
        static Configuration()
        {
            Reload();
        }

        /// <summary>
        /// Force the configuration values to be reloaded.
        /// </summary>
        internal static void Reload()
        {
            IsTraceLogEnabled = string.Equals(EnvironmentUtils.GetValue(EnvironmentVariables.EnableTraceLogs), "1");
            var disableLogPrefix = string.Equals(EnvironmentUtils.GetValue(EnvironmentVariables.DisableLogPrefix), "1");
            LogPrefix = disableLogPrefix ? string.Empty : Shared.Constants.DefaultLogPrefix;
        }

        /// <summary>
        /// Gets the log prefix for the log messages.
        /// </summary>
        internal static string? LogPrefix { get; private set; }

        /// <summary>
        /// Gets a value indicating whether trace level logging is enabled.
        /// </summary>
        internal static bool IsTraceLogEnabled { get; private set; }
    }
}
