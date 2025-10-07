// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Common;

namespace Azure.Functions.Sdk;

/// <summary>
/// Provides an implementation of the NuGet logger that routes log messages to MSBuild's logging infrastructure.
/// </summary>
/// <param name="logger">The MSBuild task logging helper used to record log messages. Cannot be null.</param>
internal sealed class MSBuildNugetLogger(TaskLoggingHelper logger)
    : NuGet.Common.ILogger
{
    private readonly TaskLoggingHelper _logger = Throw.IfNull(logger);

    private delegate void LogMessageWithDetails(string subcategory,
        string code,
        string helpKeyword,
        string? file,
        int lineNumber,
        int columnNumber,
        int endLineNumber,
        int endColumnNumber,
        MessageImportance importance,
        string message,
        params object[] messageArgs);

    private delegate void LogErrorWithDetails(string subcategory,
        string code,
        string helpKeyword,
        string? file,
        int lineNumber,
        int columnNumber,
        int endLineNumber,
        int endColumnNumber,
        string message,
        params object[] messageArgs);

    private delegate void LogMessageAsString(MessageImportance importance,
        string message,
        params object[] messageArgs);

    private delegate void LogErrorAsString(string message,
        params object[] messageArgs);

    /// <inheritdoc />
    public void Log(LogLevel level, string data)
    {
        switch (level)
        {
            case LogLevel.Warning:
                _logger.LogWarning(data);
                break;
            case LogLevel.Error:
                _logger.LogError(data);
                break;
            default:
                MessageImportance importance = Convert(level);
                _logger.LogMessage(importance, data);
                break;
        }
    }

    /// <inheritdoc />
    public void Log(ILogMessage message)
    {
        INuGetLogMessage nugetMessage = message switch
        {
            INuGetLogMessage nugetLogMessage => nugetLogMessage,
            _ => new RestoreLogMessage(message.Level, message.Message)
            {
                Code = message.Code,
                FilePath = message.ProjectPath,
                ProjectPath = message.ProjectPath,
            },
        };

        if (nugetMessage.Code == NuGetLogCode.Undefined)
        {
            Log(nugetMessage.Level, nugetMessage.Message);
        }
        else
        {
            string enumName = Enum.GetName(typeof(NuGetLogCode), nugetMessage.Code);
            switch (nugetMessage.Level)
            {
                case LogLevel.Warning:
                    _logger.LogWarning(string.Empty,
                        enumName,
                        enumName,
                        nugetMessage.FilePath,
                        nugetMessage.StartLineNumber,
                        nugetMessage.StartColumnNumber,
                        nugetMessage.EndLineNumber,
                        nugetMessage.EndColumnNumber,
                        nugetMessage.Message);
                    break;
                case LogLevel.Error:
                    _logger.LogError(string.Empty,
                        enumName,
                        enumName,
                        nugetMessage.FilePath,
                        nugetMessage.StartLineNumber,
                        nugetMessage.StartColumnNumber,
                        nugetMessage.EndLineNumber,
                        nugetMessage.EndColumnNumber,
                        nugetMessage.Message);
                    break;
                default:
                    _logger.LogMessage(string.Empty,
                        enumName,
                        enumName,
                        nugetMessage.FilePath,
                        nugetMessage.StartLineNumber,
                        nugetMessage.StartColumnNumber,
                        nugetMessage.EndLineNumber,
                        nugetMessage.EndColumnNumber,
                        Convert(nugetMessage.Level),
                        nugetMessage.Message);
                    break;
            }
        }
    }

    /// <inheritdoc />
    public System.Threading.Tasks.Task LogAsync(LogLevel level, string data)
    {
        Log(level, data);
        return System.Threading.Tasks.Task.CompletedTask;
    }

    /// <inheritdoc />
    public System.Threading.Tasks.Task LogAsync(ILogMessage message)
    {
        Log(message);
        return System.Threading.Tasks.Task.CompletedTask;
    }

    /// <inheritdoc />
    public void LogDebug(string data)
    {
        Log(LogLevel.Debug, data);
    }

    /// <inheritdoc />
    public void LogError(string data)
    {
        Log(LogLevel.Error, data);
    }

    /// <inheritdoc />
    public void LogInformation(string data)
    {
        Log(LogLevel.Information, data);
    }

    /// <inheritdoc />
    public void LogInformationSummary(string data)
    {
        LogInformation(data);
    }

    /// <inheritdoc />
    public void LogMinimal(string data)
    {
        Log(LogLevel.Minimal, data);
    }

    /// <inheritdoc />
    public void LogVerbose(string data)
    {
        Log(LogLevel.Verbose, data);
    }

    /// <inheritdoc />
    public void LogWarning(string data)
    {
        Log(LogLevel.Warning, data);
    }

    private static MessageImportance Convert(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => MessageImportance.Low,
            LogLevel.Verbose => MessageImportance.Low,
            LogLevel.Information => MessageImportance.Normal,
            LogLevel.Minimal => MessageImportance.High,
            _ => MessageImportance.Low,
        };
    }
}
