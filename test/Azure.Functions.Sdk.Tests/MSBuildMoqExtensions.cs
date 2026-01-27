// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Globalization;
using Azure.Functions.Sdk;
using Azure.Functions.Sdk.Tasks;
using Microsoft.Build.Framework;
using LogLevel = NuGet.Common.LogLevel;

namespace Moq;

internal static class MSBuildMoqExtensions
{
    public static void VerifyLog(
        this Mock<IBuildEngine> mock, LogMessage message, params string[] args)
    {
        if (message.Level == LogLevel.Error)
        {
            mock.Verify(m => m.LogErrorEvent(MatchError(message, args)), Times.Once);
        }
        else if (message.Level == LogLevel.Warning)
        {
            mock.Verify(m => m.LogWarningEvent(MatchWarning(message, args)), Times.Once);
        }
        else
        {
            mock.Verify(m => m.LogMessageEvent(MatchMessage(message, args)), Times.Once);
        }
    }

    public static void VerifyLog(
        this Mock<IBuildEngine> mock, LogLevel level, string message, params string[] args)
    {
        message = string.Format(CultureInfo.InvariantCulture, message, args);
        if (level == LogLevel.Error)
        {
            mock.Verify(m => m.LogErrorEvent(MatchError(message)), Times.Once);
        }
        else if (level == LogLevel.Warning)
        {
            mock.Verify(m => m.LogWarningEvent(MatchWarning(message)), Times.Once);
        }
        else
        {
            mock.Verify(m => m.LogMessageEvent(MatchMessage(message, level)), Times.Once);
        }
    }

    private static BuildErrorEventArgs MatchError(LogMessage message, string[] args)
    {
        return Match.Create<BuildErrorEventArgs>(e =>
        {
            return StringComparer.Ordinal.Equals(e.Message, message.Format(args))
                && message.Level == LogLevel.Error
                && e.Code == message.Code
                && e.HelpKeyword == message.HelpKeyword
                && e.HelpLink == null
                && e.Subcategory == null
                && e.LineNumber == 0
                && e.ColumnNumber == 0
                && e.EndLineNumber == 0
                && e.EndColumnNumber == 0
                && e.File == null;
        });
    }

    private static BuildWarningEventArgs MatchWarning(LogMessage message, string[] args)
    {
        return Match.Create<BuildWarningEventArgs>(e =>
        {
            return StringComparer.Ordinal.Equals(e.Message, message.Format(args))
                && message.Level == LogLevel.Warning
                && e.Code == message.Code
                && e.HelpKeyword == message.HelpKeyword
                && e.HelpLink == null
                && e.Subcategory == null
                && e.LineNumber == 0
                && e.ColumnNumber == 0
                && e.EndLineNumber == 0
                && e.EndColumnNumber == 0
                && e.File == null;
        });
    }

    private static BuildMessageEventArgs MatchMessage(LogMessage message, string[] args)
    {
        return Match.Create<BuildMessageEventArgs>(e =>
        {
            return StringComparer.Ordinal.Equals(e.Message, message.Format(args))
                && e.Importance == message.Level.ToMessageImportance()
                && e.Code == message.Code
                && e.HelpKeyword == message.HelpKeyword
                && e.Subcategory == null
                && e.LineNumber == 0
                && e.ColumnNumber == 0
                && e.EndLineNumber == 0
                && e.EndColumnNumber == 0
                && e.File == null;
        });
    }

    private static BuildErrorEventArgs MatchError(string message)
    {
        return Match.Create<BuildErrorEventArgs>(e =>
        {
            return StringComparer.Ordinal.Equals(e.Message, message)
                && e.Code == null
                && e.HelpKeyword == null
                && e.HelpLink == null
                && e.Subcategory == null
                && e.LineNumber == 0
                && e.ColumnNumber == 0
                && e.EndLineNumber == 0
                && e.EndColumnNumber == 0
                && e.File == null;
        });
    }

    private static BuildWarningEventArgs MatchWarning(string message)
    {
        return Match.Create<BuildWarningEventArgs>(e =>
        {
            return StringComparer.Ordinal.Equals(e.Message, message)
                && e.Code == null
                && e.HelpKeyword == null
                && e.HelpLink == null
                && e.Subcategory == null
                && e.LineNumber == 0
                && e.ColumnNumber == 0
                && e.EndLineNumber == 0
                && e.EndColumnNumber == 0
                && e.File == null;
        });
    }

    private static BuildMessageEventArgs MatchMessage(string message, LogLevel level)
    {
        return Match.Create<BuildMessageEventArgs>(e =>
        {
            return StringComparer.Ordinal.Equals(e.Message, message)
                && e.Importance == level.ToMessageImportance()
                && e.Code == null
                && e.HelpKeyword == null
                && e.Subcategory == null
                && e.LineNumber == 0
                && e.ColumnNumber == 0
                && e.EndLineNumber == 0
                && e.EndColumnNumber == 0
                && e.File == null;
        });
    }
}
