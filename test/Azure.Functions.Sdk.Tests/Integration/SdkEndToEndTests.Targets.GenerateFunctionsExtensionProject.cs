// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Execution;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests.Integration;

public partial class SdkEndToEndTests
{
    [Fact]
    public void Target_GenerateFunctionsExtensionProject_NoPackageRefs()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj())
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs);

        // Act
        project.Restore().Should().BeSuccessful().And.HaveNoIssues();
        TargetResult result = project.RunTarget("GenerateFunctionsExtensionProject")!;

        // Assert
        result.Should().NotBeNull();
        result.ResultCode.Should().Be(TargetResultCode.Success);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void Target_GenerateFunctionsExtensionProject_DesignTime()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj())
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs);

        // Act
        project.Restore().Should().BeSuccessful().And.HaveNoIssues();
        TargetResult result = project.RunTarget(
            "GenerateFunctionsExtensionProject", GlobalPropertiesDesignTime)!;

        // Assert
        result.Should().NotBeNull();
        result.ResultCode.Should().Be(TargetResultCode.Skipped);
        result.Items.Should().BeEmpty();
    }
}
