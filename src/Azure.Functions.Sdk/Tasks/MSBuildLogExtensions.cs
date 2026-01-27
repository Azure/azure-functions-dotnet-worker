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
                    log.LogError(
                        null,
                        message.Code!,
                        message.HelpKeyword,
                        message.HelpLink,
                        null,
                        0,
                        0,
                        0,
                        0,
                        message.RawValue,
                        args);
                    break;
                case LogLevel.Warning:
                    log.LogWarning(
                        null,
                        message.Code!,
                        message.HelpKeyword,
                        message.HelpLink,
                        null,
                        0,
                        0,
                        0,
                        0,
                        message.RawValue,
                        args);
                    break;
                case LogLevel.Minimal or LogLevel.Information or LogLevel.Verbose or LogLevel.Debug:
                    message.Level.ToMessageImportance();
                    log.LogMessage(
                        null,
                        message.Code!,
                        message.HelpKeyword,
                        null,
                        0,
                        0,
                        0,
                        0,
                        message.Level.ToMessageImportance(),
                        message.RawValue,
                        args);
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
                    log.LogError(message.RawValue, args);
                    break;
                case LogLevel.Warning:
                    log.LogWarning(message.RawValue, args);
                    break;
                case LogLevel.Minimal or LogLevel.Information or LogLevel.Verbose or LogLevel.Debug:
                    log.LogMessage(message.Level.ToMessageImportance(), message.RawValue, args);
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
