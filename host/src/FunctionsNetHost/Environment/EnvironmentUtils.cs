// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace FunctionsNetHost
{
    internal static class EnvironmentUtils
    {
        /// <summary>
        /// Gets the environment variable value.
        /// </summary>
        internal static string? GetValue(string environmentVariableName)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariableName);
            if (Logger.IsDebugLogEnabled)
            {
                Logger.LogDebug($"{environmentVariableName} environment variable value:{value}");
            }

            return value;
        }
    }
}
