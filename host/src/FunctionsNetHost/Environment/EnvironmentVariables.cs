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
    /// This env variable is used by runtime to load startup hooks.
    /// </summary>
    internal const string DotnetStartupHooks = "DOTNET_STARTUP_HOOKS";

    /// <summary>
    /// Set value to "1" for enabling worker startup hook.
    /// </summary>
    internal const string EnableWorkerStartupHook = "ENABLE_WORKER_STARTUPHOOK_ASSEMBLY";

    /// <summary>
    /// Set value to "1" for enabling extra trace logs in FunctionsNetHost.
    /// </summary>
    internal const string EnableTraceLogs = "AZURE_FUNCTIONS_FUNCTIONSNETHOST_TRACE";

    /// <summary>
    /// Application pool Id for the placeholder app. Only available in Windows(when running in IIS).
    /// </summary>
    internal const string AppPoolId  = "APP_POOL_ID";

    /// <summary>
    /// Environment variable for sending ServerUri to the worker.
    /// </summary>
    internal const string HostEndpoint = "Functions:Worker:HostEndpoint";
}
