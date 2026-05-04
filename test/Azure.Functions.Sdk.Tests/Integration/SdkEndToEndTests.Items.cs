// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests.Integration;

public partial class SdkEndToEndTests
{
    private static readonly string[] CompilerVisiblePropertiesExpected =
    [
        "FunctionsEnableMetadataSourceGen",
        "FunctionsAutoRegisterGeneratedMetadataProvider",
        "FunctionsEnableExecutorSourceGen",
        "FunctionsAutoRegisterGeneratedFunctionsExecutor",
        "FunctionsGeneratedCodeNamespace",
        "TargetFrameworkIdentifier",
        "FunctionsExecutionModel",
    ];

    [Fact]
    public void Item_LocalSettingsJson_ExpectedMetadata()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj())
            .WriteSourceFile("local.settings.json", Resources.Local_Settings_json);

        // Act
        project.TryGetItems("Content", out IReadOnlyCollection<ProjectItem>? items);

        // Assert
        items.Should().ContainSingle(x => x.EvaluatedInclude == "local.settings.json")
            .Which.Should()
            .HaveMetadata("CopyToOutputDirectory", "PreserveNewest")
            .And.HaveMetadata("CopyToPublishDirectory", "Never");
    }

    [Fact]
    public void Item_HostJson_ExpectedMetadata()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj())
            .WriteSourceFile("host.json", Resources.Host_json);

        // Act
        project.TryGetItems("Content", out IReadOnlyCollection<ProjectItem>? items);

        // Assert
        items.Should().ContainSingle(x => x.EvaluatedInclude == "host.json")
            .Which.Should()
            .HaveMetadata("CopyToOutputDirectory", "PreserveNewest")
            .And.HaveMetadata("CopyToPublishDirectory", "PreserveNewest");
    }

    [Fact]
    public void Item_ImplicitPackages_AreIncluded()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj());

        // Act
        project.TryGetItems("PackageReference", out IReadOnlyCollection<ProjectItem>? items);

        // Assert
        items.Should().ContainSingle(x => x.EvaluatedInclude == "Microsoft.Azure.Functions.Worker")
            .Which.Should().HaveMetadata("IsImplicitlyDefined", "true");

        items.Should().ContainSingle(x => x.EvaluatedInclude == "Microsoft.Azure.Functions.Worker.Sdk.Analyzers")
            .Which.Should().HaveMetadata("IsImplicitlyDefined", "true")
            .And.HaveMetadata("PrivateAssets", "all");

        items.Should().ContainSingle(x => x.EvaluatedInclude == "Microsoft.Azure.Functions.Worker.Sdk.Generators")
            .Which.Should().HaveMetadata("IsImplicitlyDefined", "true")
            .And.HaveMetadata("PrivateAssets", "all");
    }

    [Fact]
    public void Item_CompilerVisibleProperty_AreIncluded()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj());

        // Act
        project.TryGetItems("CompilerVisibleProperty", out IReadOnlyCollection<ProjectItem>? items);

        // Assert
        foreach (string expected in CompilerVisiblePropertiesExpected)
        {
            items.Should().Contain(x => x.EvaluatedInclude == expected);
        }
    }

    [Fact]
    public void Item_FunctionsCapability_IsIncluded()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj());

        // Act
        project.TryGetItems("ProjectCapability", out IReadOnlyCollection<ProjectItem>? items);

        // Assert
        items.Should().ContainSingle(x => x.EvaluatedInclude == "AzureFunctions");
    }
}
