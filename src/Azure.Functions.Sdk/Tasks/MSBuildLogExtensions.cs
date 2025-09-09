// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Common;

namespace Azure.Functions.Sdk.Tasks;

internal static class MSBuildLogExtensions
{
    public static void LogMessage(this TaskLoggingHelper log, LogMessage logCode)
        => LogMessage(log, logCode, []);

    public static void LogMessage(this TaskLoggingHelper log, LogMessage logCode, params string[] messageArgs)
    {
        if (logCode.Code is null)
        {
            log.LogNoCode(logCode, messageArgs);
        }
        else
        {
            log.LogWithCode(logCode, messageArgs);
        }
    }

    private static void LogWithCode(this TaskLoggingHelper log, LogMessage logCode, params string[] messageArgs)
    {
        log.TaskResources = Strings.ResourceManager;
        switch (logCode.Level)
        {
            case LogLevel.Error:
                log.LogErrorFromResources(
                    null,
                    logCode.Code!,
                    $"AzureFunctions.{logCode.Code}",
                    null,
                    0,
                    0,
                    0,
                    0,
                    logCode.Id,
                    messageArgs);
                break;
            case LogLevel.Warning:
                log.LogWarningFromResources(
                    null,
                    logCode.Code!,
                    $"AzureFunctions.{logCode.Code}",
                    null,
                    0,
                    0,
                    0,
                    0,
                    logCode.Id,
                    messageArgs);
                break;
            case LogLevel.Minimal:
                log.LogMessageFromResources(MessageImportance.High, logCode.Id, messageArgs);
                break;
            case LogLevel.Information:
                log.LogMessageFromResources(MessageImportance.Normal, logCode.Id, messageArgs);
                break;
            case LogLevel.Debug or LogLevel.Verbose:
                log.LogMessageFromResources(MessageImportance.Low, logCode.Id, messageArgs);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logCode.Level), logCode.Level, "Unsupported log level");
        }
    }

    private static void LogNoCode(this TaskLoggingHelper log, LogMessage logCode, params string[] messageArgs)
    {
        log.TaskResources = Strings.ResourceManager;switch (logCode.Level)
        {
            case LogLevel.Error:
                log.LogErrorFromResources(logCode.Id, messageArgs);
                break;
            case LogLevel.Warning:
                log.LogWarningFromResources(logCode.Id, messageArgs);
                break;
            case LogLevel.Minimal:
                log.LogMessageFromResources(MessageImportance.High, logCode.Id, messageArgs);
                break;
            case LogLevel.Information:
                log.LogMessageFromResources(MessageImportance.Normal, logCode.Id, messageArgs);
                break;
            case LogLevel.Debug or LogLevel.Verbose:
                log.LogMessageFromResources(MessageImportance.Low, logCode.Id, messageArgs);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logCode.Level), logCode.Level, "Unsupported log level");
        }
    }
}
