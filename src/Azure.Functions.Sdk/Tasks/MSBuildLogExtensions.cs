// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Common;

namespace Azure.Functions.Sdk.Tasks;

internal static class MSBuildLogExtensions
{
    // https://github.com/dotnet/roslyn/issues/80024
    // Using old extension method style to avoid incorrect analyzer warning.
    /// <summary>
    /// Logs a <see cref="LogMessage"/> to the MSBuild log.
    /// </summary>
    /// <param name="log">The MSBuild logger.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="args">The formatting arguments for the message.</param>
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

    extension(TaskLoggingHelper log)
    {
        /// <summary>
        /// Logs a <see cref="LogMessage"/> to the MSBuild log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogMessage(LogMessage message)
            => log.LogMessage(message, []);

        private void LogWithCode(LogMessage message, params string[] args)
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
                    throw new ArgumentOutOfRangeException(
                        nameof(message.Level), message.Level, "Unsupported log level");
            }
        }

        private void LogNoCode(LogMessage message, params string[] args)
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
                    throw new ArgumentOutOfRangeException(
                        nameof(message.Level), message.Level, "Unsupported log level");
            }
        }
    }

    extension(LogLevel level)
    {
        /// <summary>
        /// Converts the <see cref="LogLevel"/> to a corresponding <see cref="MessageImportance"/>.
        /// </summary>
        /// <returns>The <see cref="MessageImportance" />.</returns>
        public MessageImportance ToMessageImportance()
        {
            return level switch
            {
                LogLevel.Error or LogLevel.Warning or LogLevel.Minimal => MessageImportance.High,
                LogLevel.Information => MessageImportance.Normal,
                LogLevel.Debug or LogLevel.Verbose => MessageImportance.Low,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(level), level, "Unsupported log level for message importance"),
            };
        }
    }
}
