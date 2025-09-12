// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Utilities.ProjectCreation;
using Microsoft.CodeAnalysis.Text;

namespace Azure.Functions.Sdk.Tests.Integration;

internal static class MSBuildExtensions
{
    private static readonly string ThisAssemblyDirectory =
        Path.GetDirectoryName(typeof(MSBuildExtensions).Assembly.Location)!;

    private static readonly ImmutableDictionary<string, string> DefaultGlobalProperties =
        ImmutableDictionary.CreateRange(
        [
            KeyValuePair.Create("ImportDirectoryBuildProps", bool.FalseString),
            KeyValuePair.Create("ImportDirectoryPackagesProps", bool.FalseString),
            KeyValuePair.Create("ImportDirectoryBuildTargets", bool.FalseString),
            KeyValuePair.Create("AzureFunctionsSdkTasksAssembly", Path.Combine(ThisAssemblyDirectory, "Azure.Functions.Sdk.dll")),
            KeyValuePair.Create("RestoreSources", "https://api.nuget.org/v3/index.json" )
        ]);

    public static ProjectCreator AzureFunctionsProject(
        this ProjectCreatorTemplates _,
        string? path = null,
        string targetFramework = "net8.0",
        ProjectCollection? projectCollection = null,
        IDictionary<string, string>? globalProperties = null,
        Action<ProjectCreator>? configure = null)
    {
        return ProjectCreator.Create(
            path: path,
            projectCollection: projectCollection,
            globalProperties: GetGlobalProperties(globalProperties))
            .Import(Path.Combine(ThisAssemblyDirectory, "sdk", "sdk", "Sdk.props"))
            .PropertyGroup()
            .Property("TargetFramework", targetFramework)
            .CustomAction(configure)
            .Import(Path.Combine(ThisAssemblyDirectory, "sdk", "sdk", "Sdk.targets"));
    }

    public static ProjectCreator WriteSourceFile(
        this ProjectCreator project, string filePath, string text)
        => WriteSourceFile(project, filePath, SourceText.From(text));

    public static ProjectCreator WriteSourceFile(
        this ProjectCreator project, string filePath, SourceText text)
    {
        string path = Path.IsPathRooted(filePath)
            ? filePath
            : Path.Combine(Path.GetDirectoryName(project.RootElement.FullPath)!, filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text.ToString());
        return project;
    }

    public static BuildOutput Restore(this ProjectCreator project)
    {
        project.TryRestore(out _, out BuildOutput output);
        return output;
    }

    public static BuildOutput Restore(this ProjectCreator project, out IDictionary<string, TargetResult>? targets)
    {
        project.TryRestore(out _, out BuildOutput output, out targets);
        return output;
    }


    public static BuildOutput Build(this ProjectCreator project, bool restore = false)
    {
        project.TryBuild(restore, out _, out BuildOutput output);
        return output;
    }

    private static ImmutableDictionary<string, string> GetGlobalProperties(IDictionary<string, string>? overrides)
    {
        if (overrides is null || overrides.Count == 0)
        {
            return DefaultGlobalProperties;
        }

        return DefaultGlobalProperties.SetItems(overrides);
    }
}
