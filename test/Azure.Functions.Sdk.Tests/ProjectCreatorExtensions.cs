// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Azure.Functions.Sdk.Tests;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Build.Utilities.ProjectCreation;

internal static class ProjectCreatorExtensions
{
    private static readonly ImmutableDictionary<string, string> DefaultGlobalProperties =
        ImmutableDictionary.CreateRange(
        [
            KeyValuePair.Create("ImportDirectoryBuildProps", bool.FalseString),
            KeyValuePair.Create("ImportDirectoryPackagesProps", bool.FalseString),
            KeyValuePair.Create("ImportDirectoryBuildTargets", bool.FalseString),
            KeyValuePair.Create("RestoreSources", "https://api.nuget.org/v3/index.json" )
        ]);

    private static ImmutableDictionary<string, string> GetGlobalProperties(IDictionary<string, string>? overrides)
    {
        if (overrides is null || overrides.Count == 0)
        {
            return DefaultGlobalProperties;
        }

        return DefaultGlobalProperties.SetItems(overrides);
    }

    extension(ProjectCreatorTemplates _)
    {
        public ProjectCreator AzureFunctionsProject(
            string? path = null,
            string targetFramework = "net8.0",
            ProjectCollection? projectCollection = null,
            IDictionary<string, string>? globalProperties = null,
            Action<ProjectCreator>? configure = null)
        {
            return ProjectCreator.Create(
                path: path,
                projectCollection: projectCollection,
                sdk: "Azure.Functions.Sdk/99.99.99",
                globalProperties: GetGlobalProperties(globalProperties))
                .PropertyGroup()
                .Property("TargetFramework", targetFramework)
                .CustomAction(configure);
        }

        public ProjectCreator NetCoreProject(
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
    }

    extension(ProjectCreator project)
    {
        public ProjectCreator ItemPackageReference(NugetPackage package)
        {
            return project.ItemPackageReference(package.Name, package.Version);
        }

        public ProjectCreator WriteSourceFile(string filePath, string text)
        {
            return project.WriteSourceFile(filePath, SourceText.From(text));
        }

        public ProjectCreator WriteSourceFile(string filePath, SourceText text)
        {
            string path = Path.IsPathRooted(filePath)
                ? filePath
                : Path.Combine(Path.GetDirectoryName(project.RootElement.FullPath)!, filePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text.ToString());
            return project;
        }

        public BuildOutput Restore()
        {
            return project.Restore(out _);
        }

        public BuildOutput Restore(out TargetOutputs targetOutputs)
        {
            project.TryRestore(out _, out BuildOutput output, out IDictionary<string, TargetResult>? targetResults);
            targetOutputs = TargetOutputs.Create(targetResults);
            return output;
        }

        public BuildOutput Build(bool restore = false, IDictionary<string, string>? globalProperties = null)
        {
            project.TryBuild(restore, globalProperties, out _, out BuildOutput output);
            return output;
        }

        public TargetResult? RunTarget(string targetName, IDictionary<string, string>? globalProperties = null)
        {
            project.TryBuild(
                targetName, globalProperties, out _, out _, out IDictionary<string, TargetResult>? targetResults);
            if (targetResults is null || !targetResults.TryGetValue(targetName, out TargetResult? result))
            {
                return null;
            }

            return result;
        }
    }
}
