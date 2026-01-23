// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using AwesomeAssertions.Execution;
using AwesomeAssertions.Formatting;
using AwesomeAssertions.Primitives;
using Azure.Functions.Sdk.Tasks;
using Microsoft.Build.Framework;
using NuGet.Common;

using LogMessage = Azure.Functions.Sdk.LogMessage;
using LogWrapper = (Azure.Functions.Sdk.LogMessage Log, string[] Args);

namespace AwesomeAssertions;

internal static partial class AssertionExtensions
{
    public static BuildEventArgsAssertions Should(this BuildEventArgs instance)
        => new(instance, AssertionChain.GetOrCreate());
}

internal class BuildEventArgsAssertions(BuildEventArgs subject, AssertionChain assertionChain)
    : ObjectAssertions<BuildEventArgs, BuildEventArgsAssertions>(subject, assertionChain), IProvidesFormatter
{
    private readonly AssertionChain _chain = assertionChain;

    protected override string Identifier => "BuildEventArgs";

    public static IValueFormatter CreateFormatter() => new Formatter();

    [CustomAssertion]
    public AndConstraint<BuildEventArgsAssertions> HaveSender(
        string sender, string because = "", params object[] becauseArgs)
    {
        _chain
            .ForCondition(Subject?.SenderName is string actualSender && actualSender == sender)
            .BecauseOf(because, becauseArgs)
            .FailWith(
                "Expected {context:BuildEventArgs} to have sender {0}{reason}, but found {1}.",
                sender,
                Subject?.SenderName ?? "<null>");
        return new AndConstraint<BuildEventArgsAssertions>(this);
    }

    [CustomAssertion]
    public AndConstraint<BuildEventArgsAssertions> BeSdkMessage(
        LogMessage log, string because = "", params object[] becauseArgs)
    {
        return BeSdkMessage((log, []), because, becauseArgs);
    }

    [CustomAssertion]
    public AndConstraint<BuildEventArgsAssertions> BeSdkMessage(
        (LogMessage Log, string Arg) log, string because = "", params object[] becauseArgs)
    {
        return BeSdkMessage((log.Log, [log.Arg]), because, becauseArgs);
    }

    [CustomAssertion]
    public AndConstraint<BuildEventArgsAssertions> BeSdkMessage(
        LogWrapper log, string because = "", params object[] becauseArgs)
    {
        switch (log.Log.Level)
        {
            case LogLevel.Error:
                BeOfType<BuildErrorEventArgs>(because, becauseArgs);
                break;
            case LogLevel.Warning:
                BeOfType<BuildWarningEventArgs>(because, becauseArgs);
                break;
            default:
                BeOfType<BuildMessageEventArgs>(because, becauseArgs);
                break;
        }

        using AssertionScope scope = new("The BuildErrorEventArgs should match the LogMessage.");
        GetCode(Subject).Should().Be(log.Log.Code, because, becauseArgs);
        Subject.HelpKeyword.Should().Be(log.Log.HelpKeyword, because, becauseArgs);
        Subject.Message.Should().Be(log.Log.Format(log.Args), because, becauseArgs);

        if (Subject is BuildMessageEventArgs message)
        {
            message.Importance.Should().Be(log.Log.Level.ToMessageImportance(), because, becauseArgs);
        }

        return new AndConstraint<BuildEventArgsAssertions>(this);
    }

    private static string? GetCode(BuildEventArgs args) => args switch
    {
        BuildErrorEventArgs error => error.Code,
        BuildWarningEventArgs warning => warning.Code,
        BuildMessageEventArgs message => message.Code,
        _ => null,
    };

    private class Formatter : IValueFormatter
    {
        public bool CanHandle(object value) => value is BuildEventArgs;

        public void Format(object value, FormattedObjectGraph formattedGraph, FormattingContext context, FormatChild formatChild)
        {
            BuildEventArgs item = (BuildEventArgs)value;

            string result = $"[{GetTypeNameShort(item)}] {item.Message}";
            string sender = item.SenderName ?? "<null>";

            if (context.UseLineBreaks)
            {
                formattedGraph.AddLine(result);
                using IDisposable indent = formattedGraph.WithIndentation();
                formattedGraph.AddLine($"Sender: {sender}");
            }
            else
            {
                formattedGraph.AddFragment(result);
                formattedGraph.AddFragment($" (Sender: {sender})");
            }
        }

        private static string GetTypeNameShort(BuildEventArgs item)
        {
            return item switch
            {
                BuildErrorEventArgs => "error",
                BuildWarningEventArgs => "warning",
                BuildMessageEventArgs m => m.Importance.ToString().ToLowerInvariant(),
                _ => item.GetType().Name,
            };
        }
    }
}
