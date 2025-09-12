// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using AwesomeAssertions.Execution;
using AwesomeAssertions.Formatting;
using AwesomeAssertions.Primitives;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities.ProjectCreation;

namespace AwesomeAssertions;

internal static partial class AssertionExtensions
{
    public static BuildOutputAssertions Should(this BuildOutput instance)
        => new(instance, AssertionChain.GetOrCreate());
}

internal class BuildOutputAssertions(BuildOutput subject, AssertionChain assertionChain)
    : ObjectAssertions<BuildOutput, BuildOutputAssertions>(subject, assertionChain), IProvidesFormatter
{
    private readonly AssertionChain _chain = assertionChain;

    protected override string Identifier => "BuildOutput";

    public static IValueFormatter CreateFormatter() => new Formatter();

    [CustomAssertion]
    public AndConstraint<BuildOutputAssertions> BeSuccessful(string because = "", params object[] becauseArgs)
    {
        _chain
            .ForCondition(Subject.Succeeded.GetValueOrDefault(false))
            .BecauseOf(because, becauseArgs)
            .FailWith(
                "Expected {context:BuildOutput} to be successful{reason}, but it was not. Build logs: {0}",
                Subject.GetConsoleLog());
        return new AndConstraint<BuildOutputAssertions>(this);
    }

    [CustomAssertion]
    public AndConstraint<BuildOutputAssertions> BeFailed(string because = "", params object[] becauseArgs)
    {
        _chain
            .ForCondition(!Subject.Succeeded.GetValueOrDefault(false))
            .BecauseOf(because, becauseArgs)
            .FailWith(
                "Expected {context:BuildOutput} to be failed{reason}, but it was not. Build logs: {0}",
                Subject.GetConsoleLog());
        return new AndConstraint<BuildOutputAssertions>(this);
    }

    [CustomAssertion]
    public AndConstraint<BuildOutputAssertions> HaveNoIssues(string because = "", params object[] becauseArgs)
    {
        return HaveNoErrors(because, becauseArgs).And.HaveNoWarnings(because, becauseArgs);
    }

    [CustomAssertion]
    public AndConstraint<BuildOutputAssertions> HaveNoErrors(string because = "", params object[] becauseArgs)
    {
        _chain
            .ForCondition(Subject.ErrorEvents.Count == 0)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:BuildOutput} to have no errors{reason}, but found {0}.", Subject.ErrorEvents.Count);
        return new AndConstraint<BuildOutputAssertions>(this);
    }

    [CustomAssertion]
    public AndConstraint<BuildOutputAssertions> HaveNoWarnings(string because = "", params object[] becauseArgs)
    {
        _chain
            .ForCondition(Subject.WarningEvents.Count == 0)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:BuildOutput} to have no warnings{reason}, but found {0}.", Subject.WarningEvents.Count);
        return new AndConstraint<BuildOutputAssertions>(this);
    }

    [CustomAssertion]
    public AndWhichConstraint<BuildOutputAssertions, BuildErrorEventArgs> HaveSingleError(string because = "", params object[] becauseArgs)
    {
        _chain
            .ForCondition(Subject.ErrorEvents.Count == 1)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:BuildOutput} to have a single error{reason}, but found {0}.", Subject.ErrorEvents.Count);
        return new AndWhichConstraint<BuildOutputAssertions, BuildErrorEventArgs>(this, Subject.ErrorEvents.Single());
    }

    [CustomAssertion]
    public AndWhichConstraint<BuildOutputAssertions, BuildWarningEventArgs> HaveSingleWarning(string because = "", params object[] becauseArgs)
    {
        _chain
            .ForCondition(Subject.WarningEvents.Count == 1)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:BuildOutput} to have a single warning{reason}, but found {0}.", Subject.WarningEvents.Count);
        return new AndWhichConstraint<BuildOutputAssertions, BuildWarningEventArgs>(this, Subject.WarningEvents.Single());
    }

    private class Formatter : IValueFormatter
    {
        public bool CanHandle(object value) => value is BuildOutput;

        public void Format(object value, FormattedObjectGraph formattedGraph, FormattingContext context, FormatChild formatChild)
        {
            BuildOutput output = (BuildOutput)value;
            if (context.UseLineBreaks)
            {
                formattedGraph.AddLine($"[BuildOutput] Succeeded: {output.Succeeded}");
                using IDisposable disposable = formattedGraph.WithIndentation();

                if (output.ErrorEvents.Count > 0)
                {
                    formattedGraph.AddLine($"Errors: {output.ErrorEvents.Count}");
                }

                if (output.WarningEvents.Count > 0)
                {
                    formattedGraph.AddLine($"Warnings: {output.WarningEvents.Count}");
                }

                foreach ((string key, bool result) in output.ProjectResults)
                {
                    formattedGraph.AddLine($"Project: {key} - Succeeded: {result}");
                }
            }
            else
            {
                formattedGraph.AddFragment($"[BuildOutput] Succeeded: {output.Succeeded}");

                if (output.ErrorEvents.Count > 0)
                {
                    formattedGraph.AddFragment($" (Errors: {output.ErrorEvents.Count})");
                }

                if (output.WarningEvents.Count > 0)
                {
                    formattedGraph.AddFragment($" (Warnings: {output.WarningEvents.Count})");
                }
            }
        }
    }
}
