// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace FunctionsNetHost.Shared
{
    public static class EnvironmentVariables
    {
        /// <summary>
        /// The environment variable which is used to specify the path to the jittrace file which will be used for prejitting.
        /// </summary>
        public const string PreJitFilePath = "AZURE_FUNCTIONS_NETHOST_PREJIT_FILE_PATH";

        /// <summary>
        /// The .NET startup hooks environment variable.
        /// </summary>
        public const string DotnetStartupHooks = "DOTNET_STARTUP_HOOKS";
    }
}
