// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests.Integration;

public partial class SdkEndToEndTests : MSBuildSdkTestBase
{
    // (build: false, restore: true) is not a valid combination.
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void Publish_Success(bool build, bool restore)
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(), targetFramework: "net8.0")
            .Property("AssemblyName", "MyFunctionApp")
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs);

        if (!restore)
        {
            // restore separately if neeeded.
            project.Restore().Should().BeSuccessful().And.HaveNoIssues();
        }

        if (!build)
        {
            // build separately if needed.
            project.Build(restore).Should().BeSuccessful().And.HaveNoIssues();
        }

        // Act
        BuildOutput output = project.Publish(build, restore);

        // Assert
        output.Should().BeSuccessful().And.HaveNoIssues();
        string outputPath = project.GetPublishPath();
        ValidateConfig(
            Path.Combine(outputPath, "worker.config.json"),
            "dotnet",
            "MyFunctionApp.dll");
        ValidateExtensionsPayload(outputPath, "function.deps.json");
        ValidateExtensionJson(outputPath, []);
    }
}
