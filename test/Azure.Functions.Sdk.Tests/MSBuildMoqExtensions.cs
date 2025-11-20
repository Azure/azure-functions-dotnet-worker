// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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
}
