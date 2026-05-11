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
            // restore separately if needed.
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
        ValidateExtensionsPayload(outputPath, MinExpectedExtensionFiles);
        ValidateExtensionJson(outputPath, WebJobsExtension.MetadataLoader);
    }

    [Fact]
    public void Publish_NoBuild_SelfContained_GeneratesFreshWorkerConfig()
    {
        // Arrange: build with RID but without self-contained (config gets "dotnet")
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(), targetFramework: "net8.0")
            .Property("AssemblyName", "MyFunctionApp")
            .Property("RuntimeIdentifier", "win-x64")
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs);

        Dictionary<string, string> buildProperties = new()
        {
            ["SelfContained"] = "true",
        };

        project.Build(restore: true, buildProperties).Should().BeSuccessful().And.HaveNoIssues();
        ValidateConfig(
            Path.Combine(project.GetOutputPath(), "worker.config.json"),
            "{WorkerRoot}MyFunctionApp.exe",
            "MyFunctionApp.dll");

        // Act: publish with --no-build --self-contained false (same RID, removing SelfContained)
        Dictionary<string, string> publishProperties = new()
        {
            ["NoBuild"] = "true",
            ["SelfContained"] = "false",
        };

        BuildOutput output = project.Publish(build: false, restore: false, publishProperties);

        // Assert: publish output should not have self-contained config
        output.Should().BeSuccessful().And.HaveNoIssues();
        string publishPath = project.GetPublishPath();
        ValidateConfig(
            Path.Combine(publishPath, "worker.config.json"),
            "dotnet",
            "MyFunctionApp.dll");
    }
}
