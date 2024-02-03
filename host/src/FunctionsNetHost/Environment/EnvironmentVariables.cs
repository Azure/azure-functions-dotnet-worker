// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace FunctionsNetHost;

internal static class EnvironmentVariables
{
    /// <summary>
    /// Set value to "1" for enabling extra trace logs in FunctionsNetHost.
    /// </summary>
    internal const string FunctionsNetHostTrace = "AZURE_FUNCTIONS_FUNCTIONSNETHOST_TRACE";

    /// <summary>
    /// Application pool Id for the placeholder app. Only available in Windows(when running in IIS).
    /// </summary>
    internal const string AppPoolId  = "APP_POOL_ID";
}
