// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace FunctionsNetHost;

internal static class EnvironmentVariables
{
    /// <summary>
    /// Set value to "1" will prevent the log entries to have the prefix "LanguageWorkerConsoleLog".
    /// Set this to see logs when you are debugging FunctionsNetHost locally with WebHost.
    /// </summary>
    internal const string DisableLogPrefix = "AZURE_FUNCTIONS_FUNCTIONSNETHOST_DISABLE_LOGPREFIX";

    /// <summary>
    /// Set value to "1" for enabling additional trace logs in FunctionsNetHost.
    /// </summary>
    internal const string EnableTraceLogs = "AZURE_FUNCTIONS_FUNCTIONSNETHOST_TRACE";

    /// <summary>
    /// Set value to "1" to enable delayed .NET host trace capture in FunctionsNetHost.
    /// </summary>
    internal const string EnableHostTrace = "FUNCTIONS_NET_HOST_ENABLE_HOST_TRACE";

    internal const string ContainerName = "CONTAINER_NAME";

    internal const string StampName = "WEBSITE_HOME_STAMPNAME";

    internal const string TenantId = "WEBSITE_STAMP_DEPLOYMENT_ID";

    /// <summary>
    /// Application pool Id for the placeholder app. Only available in Windows(when running in IIS).
    /// </summary>
    internal const string AppPoolId  = "APP_POOL_ID";

    /// <summary>
    /// The worker runtime version. Example value: "8.0" (for a .NET8 placeholder)
    /// </summary>
    internal const string FunctionsWorkerRuntimeVersion = "FUNCTIONS_WORKER_RUNTIME_VERSION";
}
