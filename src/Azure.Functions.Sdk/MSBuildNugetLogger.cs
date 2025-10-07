// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Common;

namespace Azure.Functions.Sdk;

internal sealed class MSBuildNugetLogger(TaskLoggingHelper logger) : NuGet.Common.ILogger
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

    public System.Threading.Tasks.Task LogAsync(LogLevel level, string data)
    {
        Log(level, data);
        return System.Threading.Tasks.Task.CompletedTask;
    }

    public System.Threading.Tasks.Task LogAsync(ILogMessage message)
    {
        Log(message);
        return System.Threading.Tasks.Task.CompletedTask;
    }

    public void LogDebug(string data)
    {
        Log(LogLevel.Debug, data);
    }

    public void LogError(string data)
    {
        Log(LogLevel.Error, data);
    }

    public void LogInformation(string data)
    {
        Log(LogLevel.Information, data);
    }

    public void LogInformationSummary(string data)
    {
        LogInformation(data);
    }

    public void LogMinimal(string data)
    {
        Log(LogLevel.Minimal, data);
    }

    public void LogVerbose(string data)
    {
        Log(LogLevel.Verbose, data);
    }

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

    private void LogError(INuGetLogMessage logMessage,
        LogErrorWithDetails logWithDetails,
        LogErrorAsString logAsString)
    {
        if (logMessage.Code > NuGetLogCode.Undefined)
        {
            // NuGet does not currently have a subcategory while throwing logs, hence string.Empty
            logWithDetails(string.Empty,
                Enum.GetName(typeof(NuGetLogCode), logMessage.Code),
                Enum.GetName(typeof(NuGetLogCode), logMessage.Code),
                logMessage.FilePath,
                logMessage.StartLineNumber,
                logMessage.StartColumnNumber,
                logMessage.EndLineNumber,
                logMessage.EndColumnNumber,
                logMessage.Message);
        }
        else
        {
            logAsString(logMessage.Message);
        }
    }
}
