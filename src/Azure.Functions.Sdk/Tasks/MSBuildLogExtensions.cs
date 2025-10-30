// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Common;

namespace Azure.Functions.Sdk.Tasks;

internal static class MSBuildLogExtensions
{
    public static void LogMessage(this TaskLoggingHelper log, LogMessage message)
        => LogMessage(log, message, []);

    public static void LogMessage(this TaskLoggingHelper log, LogMessage message, params string[] args)
    {
        if (message.Code is null)
        {
            log.LogNoCode(message, args);
        }
        else
        {
            log.LogWithCode(message, args);
        }
    }

    public static MessageImportance ToMessageImportance(this LogLevel level)
        => level switch
        {
            LogLevel.Error or LogLevel.Warning or LogLevel.Minimal => MessageImportance.High,
            LogLevel.Information => MessageImportance.Normal,
            LogLevel.Debug or LogLevel.Verbose => MessageImportance.Low,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unsupported log level for message importance"),
        };

    private static void LogWithCode(this TaskLoggingHelper log, LogMessage message, params string[] args)
    {
        log.TaskResources = Strings.ResourceManager;
        switch (message.Level)
        {
            case LogLevel.Error:
                log.LogErrorFromResources(
                    null,
                    message.Code!,
                    message.HelpKeyword,
                    null,
                    0,
                    0,
                    0,
                    0,
                    message.Id,
                    args);
                break;
            case LogLevel.Warning:
                log.LogWarningFromResources(
                    null,
                    message.Code!,
                    message.HelpKeyword,
                    null,
                    0,
                    0,
                    0,
                    0,
                    message.Id,
                    args);
                break;
            case LogLevel.Minimal:
                log.LogMessageFromResources(MessageImportance.High, message.Id, args);
                break;
            case LogLevel.Information:
                log.LogMessageFromResources(MessageImportance.Normal, message.Id, args);
                break;
            case LogLevel.Debug or LogLevel.Verbose:
                log.LogMessageFromResources(MessageImportance.Low, message.Id, args);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(message.Level), message.Level, "Unsupported log level");
        }
    }

    private static void LogNoCode(this TaskLoggingHelper log, LogMessage message, params string[] args)
    {
        log.TaskResources = Strings.ResourceManager;
        switch (message.Level)
        {
            case LogLevel.Error:
                log.LogErrorFromResources(message.Id, args);
                break;
            case LogLevel.Warning:
                log.LogWarningFromResources(message.Id, args);
                break;
            case LogLevel.Minimal:
                log.LogMessageFromResources(MessageImportance.High, message.Id, args);
                break;
            case LogLevel.Information:
                log.LogMessageFromResources(MessageImportance.Normal, message.Id, args);
                break;
            case LogLevel.Debug or LogLevel.Verbose:
                log.LogMessageFromResources(MessageImportance.Low, message.Id, args);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(message.Level), message.Level, "Unsupported log level");
        }
    }
}
