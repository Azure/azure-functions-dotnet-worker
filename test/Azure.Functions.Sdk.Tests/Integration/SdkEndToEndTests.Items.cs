// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using AwesomeAssertions;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests.Integration;

public partial class SdkEndToEndTests
{
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
    public void Item_WorkerPackage_IsIncluded()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj());

        // Act
        project.TryGetItems("PackageReference", out IReadOnlyCollection<ProjectItem>? items);

        // Assert
        items.Should().ContainSingle(x => x.EvaluatedInclude == "Microsoft.Azure.Functions.Worker")
            .Which.Should()
            .HaveMetadata("Version", "2.2.0")
            .And.HaveMetadata("IsImplicitlyDefined", "true");
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
