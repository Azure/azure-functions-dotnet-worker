﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Build.Utilities.ProjectCreation;

public static class ProjectCreatorExtensions
{
    private static readonly string ThisAssemblyDirectory =
        Path.GetDirectoryName(typeof(ProjectCreatorExtensions).Assembly.Location)!;

    private static readonly ImmutableDictionary<string, string> DefaultGlobalProperties =
        ImmutableDictionary.CreateRange(
        [
            KeyValuePair.Create("ImportDirectoryBuildProps", bool.FalseString),
            KeyValuePair.Create("ImportDirectoryPackagesProps", bool.FalseString),
            KeyValuePair.Create("ImportDirectoryBuildTargets", bool.FalseString),
            KeyValuePair.Create("AzureFunctionsSdkTasksAssembly", Path.Combine(ThisAssemblyDirectory, "Azure.Functions.Sdk.dll"))
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

    public static ProjectCreator NetCoreProject(
        this ProjectCreatorTemplates _,
        string? path = null,
        string targetFramework = "net8.0",
        ProjectCollection? projectCollection = null,
        IDictionary<string, string>? globalProperties = null,
        Action<ProjectCreator>? configure = null)
    {
        return ProjectCreator.Templates.SdkCsproj(
            path: path,
            targetFramework: targetFramework,
            projectCreator: configure,
            projectCollection: projectCollection,
            globalProperties: GetGlobalProperties(globalProperties));
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

    public static BuildOutput Restore(this ProjectCreator project) => project.Restore(out _);

    public static BuildOutput Restore(this ProjectCreator project, out TargetOutputs targetOutputs)
    {
        project.TryRestore(out _, out BuildOutput output, out IDictionary<string, TargetResult>? targetResults);
        targetOutputs = TargetOutputs.Create(targetResults);
        return output;
    }

    public static BuildOutput Build(this ProjectCreator project, bool restore = false, IDictionary<string, string>? globalProperties = null)
    {
        project.TryBuild(restore, globalProperties, out _, out BuildOutput output);
        return output;
    }

    public static TargetResult? RunTarget(this ProjectCreator project, string targetName, IDictionary<string, string>? globalProperties = null)
    {
        project.TryBuild(targetName, globalProperties, out _, out _, out IDictionary<string, TargetResult>? targetResults);
        if (targetResults is null || !targetResults.TryGetValue(targetName, out TargetResult? result))
        {
            return null;
        }

        return result;
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
