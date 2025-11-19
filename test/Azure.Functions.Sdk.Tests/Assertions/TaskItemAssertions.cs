// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using AwesomeAssertions.Execution;
using AwesomeAssertions.Formatting;
using AwesomeAssertions.Primitives;
using Microsoft.Build.Framework;

namespace AwesomeAssertions;

internal static partial class AssertionExtensions
{
    public static TaskItemAssertions Should(this ITaskItem instance)
        => new(instance, AssertionChain.GetOrCreate());
}

internal class TaskItemAssertions(ITaskItem subject, AssertionChain assertionChain)
    : ObjectAssertions<ITaskItem, TaskItemAssertions>(subject, assertionChain), IProvidesFormatter
{
    private readonly AssertionChain _chain = assertionChain;

    protected override string Identifier => "TaskItem";

    public static IValueFormatter CreateFormatter() => new Formatter();

    [CustomAssertion]
    public AndConstraint<TaskItemAssertions> HaveItemSpec(
        string expected, string because = "", params object[] becauseArgs)
    {
        return HaveItemSpec(expected, StringComparer.Ordinal, because, becauseArgs);
    }

    [CustomAssertion]
    public AndConstraint<TaskItemAssertions> HaveItemSpec(
        string expected, IEqualityComparer<string> comparer, string because = "", params object[] becauseArgs)
    {
        NotBeNull(because, becauseArgs);

        string actual = Subject.ItemSpec;
        _chain
            .ForCondition(comparer.Equals(actual, expected))
            .BecauseOf(because, becauseArgs)
            .FailWith(
                "Expected {context:TaskItem} to have ItemSpec {0}{reason}, but found {1}.",
                expected,
                actual);

        return new AndConstraint<TaskItemAssertions>(this);
    }

    [CustomAssertion]
    public AndConstraint<TaskItemAssertions> HaveMetadata(
        string name, string value, string because = "", params object[] becauseArgs)
    {
        return HaveMetadata(name, value, StringComparer.Ordinal, because, becauseArgs);
    }

    [CustomAssertion]
    public AndConstraint<TaskItemAssertions> HaveMetadata(
        string name,
        string value,
        IEqualityComparer<string> comparer,
        string because = "",
        params object[] becauseArgs)
    {
        NotBeNull(because, becauseArgs);

        string? actual = Subject.GetMetadata(name);
        _chain
            .ForCondition(comparer.Equals(actual, value))
            .BecauseOf(because, becauseArgs)
            .FailWith(
                "Expected {context:ProjectItem} to have metadata {0} with value {1}{reason}, but found {2}.",
                name,
                value,
                actual ?? "<null>");
        return new AndConstraint<TaskItemAssertions>(this);
    }

    private class Formatter : IValueFormatter
    {
        public bool CanHandle(object value) => value is ITaskItem;

        public void Format(
            object value, FormattedObjectGraph formattedGraph, FormattingContext context, FormatChild formatChild)
        {
            ITaskItem item = (ITaskItem)value;
            if (context.UseLineBreaks)
            {
                formattedGraph.AddLine(item.ItemSpec);
            }
            else
            {
                formattedGraph.AddFragment(item.ItemSpec);
            }
        }
    }
}
