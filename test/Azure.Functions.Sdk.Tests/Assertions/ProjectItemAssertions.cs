// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using AwesomeAssertions.Execution;
using AwesomeAssertions.Formatting;
using AwesomeAssertions.Primitives;
using Microsoft.Build.Evaluation;

namespace AwesomeAssertions;

internal static partial class AssertionExtensions
{
    public static ProjectItemAssertions Should(this ProjectItem instance)
        => new(instance, AssertionChain.GetOrCreate());
}

internal class ProjectItemAssertions(ProjectItem subject, AssertionChain assertionChain)
    : ObjectAssertions<ProjectItem, ProjectItemAssertions>(subject, assertionChain), IProvidesFormatter
{
    private readonly AssertionChain _chain = assertionChain;

    protected override string Identifier => "ProjectItem";

    public static IValueFormatter CreateFormatter() => new Formatter();

    [CustomAssertion]
    public AndConstraint<ProjectItemAssertions> HaveIdentity(
        string identity, string because = "", params object[] becauseArgs)
        => HaveIdentity(identity, StringComparer.Ordinal, because, becauseArgs);

    [CustomAssertion]
    public AndConstraint<ProjectItemAssertions> HaveIdentity(
        string identity, IEqualityComparer<string> comparer, string because = "", params object[] becauseArgs)
    {
        string actual = Subject.EvaluatedInclude;
        _chain
            .ForCondition(comparer.Equals(actual, identity))
            .BecauseOf(because, becauseArgs)
            .FailWith(
                "Expected {context:ProjectItem} to have identity {0}{reason}, but found {1}.",
                identity,
                actual);

        return new AndConstraint<ProjectItemAssertions>(this);
    }

    [CustomAssertion]
    public AndConstraint<ProjectItemAssertions> HaveMetadata(
        string name, string value, string because = "", params object[] becauseArgs)
    {
        return HaveMetadata(name, value, StringComparer.Ordinal, because, becauseArgs);
    }

    [CustomAssertion]
    public AndConstraint<ProjectItemAssertions> HaveMetadata(
        string name,
        string value,
        IEqualityComparer<string> comparer,
        string because = "",
        params object[] becauseArgs)
    {
        string? actual = Subject.GetMetadataValue(name);
        _chain
            .ForCondition(comparer.Equals(actual, value))
            .BecauseOf(because, becauseArgs)
            .FailWith(
                "Expected {context:ProjectItem} to have metadata {0} with value {1}{reason}, but found {2}.",
                name,
                value,
                actual ?? "<null>");

        return new AndConstraint<ProjectItemAssertions>(this);
    }

    private class Formatter : IValueFormatter
    {
        public bool CanHandle(object value) => value is ProjectItem;

        public void Format(object value, FormattedObjectGraph formattedGraph, FormattingContext context, FormatChild formatChild)
        {
            ProjectItem item = (ProjectItem)value;
            string result = $"{item.ItemType}={item.EvaluatedInclude}";

            if (context.UseLineBreaks)
            {
                formattedGraph.AddLine(result);
            }
            else
            {
                formattedGraph.AddFragment(result);
            }
        }
    }
}
