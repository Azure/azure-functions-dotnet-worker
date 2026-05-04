// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests.Integration;

public partial class SdkEndToEndTests
{
    [Fact]
    public void Target_InnerBuild_Warning()
    {
        // Arrange
        // Create a normal Azure Functions project that will generate the inner azure_functions.g.csproj.
        // A custom target adds the generated project as a ProjectReference, simulating a traversal
        // project that accidentally globs it in. Building the outer project should fail because
        // the inner project's _BlockBuild target fires.
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj())
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs)
            .ItemProjectReference("**/azure_functions.g.csproj");

        // Act
        project.Restore().Should().BeSuccessful().And.HaveNoIssues();
        BuildOutput output = project.Build();

        // Assert
        output.Should().BeSuccessful()
            .And.HaveSingleWarning()
            .Which.Should().BeSdkMessage(LogMessage.Warning_GeneratedProjectShouldNotBeBuilt)
            .And.HaveSender("FuncSdkLog");
    }
}
