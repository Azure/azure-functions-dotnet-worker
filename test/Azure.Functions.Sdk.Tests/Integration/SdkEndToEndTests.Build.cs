// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests.Integration;

public partial class SdkEndToEndTests
{
    [Fact]
    public void Build_NetCore()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(), targetFramework: "net8.0")
            .Property("AssemblyName", "MyFunctionApp")
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs);

        // Act
        BuildOutput output = project.Build(restore: true);

        // Assert
        output.Should().BeSuccessful().And.HaveNoIssues();
        string outputPath = project.GetOutputPath();
        ValidateConfig(
            Path.Combine(outputPath, "worker.config.json"),
            "dotnet",
            "MyFunctionApp.dll");
    }

    [Fact]
    public void Build_NetFx()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(), targetFramework: "net481")
            .Property("AssemblyName", "MyFunctionApp")
            .Property("LangVersion", "latest")
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs);

        // Act
        BuildOutput output = project.Build(restore: true);

        // Assert
        output.Should().BeSuccessful().And.HaveNoIssues();
        string outputPath = project.GetOutputPath();
        ValidateConfig(
            Path.Combine(outputPath, "worker.config.json"),
            "{WorkerRoot}MyFunctionApp.exe",
            "MyFunctionApp.exe");
    }

    [Fact]
    public void Build_Incremental_NoOp()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(), targetFramework: "net8.0")
            .Property("AssemblyName", "MyFunctionApp")
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs);

        // Act
        BuildOutput output = project.Build(restore: true);

        // Assert
        output.Should().BeSuccessful().And.HaveNoIssues();
        string outputPath = project.GetOutputPath();

        string configPath = Path.Combine(outputPath, "worker.config.json");
        ValidateConfig(configPath, "dotnet", "MyFunctionApp.dll");

        FileInfo config = new(configPath);
        DateTime lastWriteTime = config.LastWriteTimeUtc;

        // Act 2: Incremental build
        BuildOutput output2 = project.Build();

        // Assert 2: Verify no changes were made
        output2.Should().BeSuccessful().And.HaveNoIssues();
        config.Refresh();
        config.LastWriteTimeUtc.Should().Be(lastWriteTime);
    }
}
