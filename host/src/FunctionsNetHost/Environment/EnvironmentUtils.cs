// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace FunctionsNetHost
{
    internal static class EnvironmentUtils
    {
#if OS_LINUX
        [System.Runtime.InteropServices.DllImport("libc")]
        private static extern int setenv(string name, string value, int overwrite);

        [System.Runtime.InteropServices.DllImport("libc")]
        private static extern string getenv(string name);
#endif

        /// <summary>
        /// Gets the environment variable value.
        /// </summary>
        internal static string? GetValue(string environmentVariableName)
        {
            // Observed Environment.GetEnvironmentVariable not returning the value which was just set. So using native method directly here.
#if OS_LINUX
            return getenv(environmentVariableName);
#else
            return Environment.GetEnvironmentVariable(environmentVariableName);
#endif
        }

        /// <summary>
        /// Sets the environment variable value.
        /// </summary>
        internal static void SetValue(string name, string value)
        {
            /*
             *  Environment.SetEnvironmentVariable is not setting the value of the parent process in Unix.
             *  So using the native method directly here.
             * */
#if OS_LINUX
            setenv(name, value, 1);
#else
            Environment.SetEnvironmentVariable(name, value);
#endif
        }
    }
}
