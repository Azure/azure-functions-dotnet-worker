// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests.Integration;

public partial class SdkEndToEndTests
{
    [Fact]
    public void Clean_RemovesFunctionsArtifacts()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj())
            .Property("AssemblyName", "MyFunctionApp")
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs);

        project.Build(restore: true).Should().BeSuccessful().And.HaveNoIssues();

        string outputPath = project.GetOutputPath();
        string intermediatePath = project.GetIntermediateOutputPath();
        string extensionProjectDir = project.GetFunctionsExtensionProjectDirectory();

        // Verify artifacts exist after build.
        File.Exists(Path.Combine(outputPath, "worker.config.json")).Should().BeTrue("worker.config.json should exist in output after build");
        File.Exists(Path.Combine(outputPath, "extensions.json")).Should().BeTrue("extensions.json should exist in output after build");
        Directory.Exists(Path.Combine(outputPath, Constants.ExtensionsOutputFolder)).Should().BeTrue(".azurefunctions should exist in output after build");
        File.Exists(Path.Combine(intermediatePath, "worker.config.json")).Should().BeTrue("worker.config.json should exist in intermediate after build");
        File.Exists(Path.Combine(intermediatePath, "extensions.json")).Should().BeTrue("extensions.json should exist in intermediate after build");
        File.Exists(Path.Combine(intermediatePath, "extensions.json.hash")).Should().BeTrue("extensions.json.hash should exist in intermediate after build");
        Directory.Exists(extensionProjectDir).Should().BeTrue("extension project directory should exist after build");

        // Act
        BuildOutput cleanOutput = project.Clean();

        // Assert
        cleanOutput.Should().BeSuccessful();
        File.Exists(Path.Combine(intermediatePath, "worker.config.json")).Should().BeFalse("worker.config.json should be cleaned from intermediate");
        File.Exists(Path.Combine(intermediatePath, "extensions.json")).Should().BeFalse("extensions.json should be cleaned from intermediate");
        File.Exists(Path.Combine(intermediatePath, "extensions.json.hash")).Should().BeFalse("extensions.json.hash should be cleaned from intermediate");
        Directory.Exists(extensionProjectDir).Should().BeFalse("extension project directory should be cleaned");
        Directory.Exists(Path.Combine(outputPath, Constants.ExtensionsOutputFolder)).Should().BeFalse(".azurefunctions should be cleaned from output");
    }

    [Fact]
    public void Clean_MultiTarget_RemovesFunctionsArtifacts()
    {
        // Arrange
        string[] targetFrameworks = ["net8.0", "net481"];
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(), targetFramework: null)
            .TargetFrameworks(targetFrameworks)
            .Property("AssemblyName", "MyFunctionApp")
            .Property("LangVersion", "latest")
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs);

        project.Build(restore: true).Should().BeSuccessful().And.HaveNoIssues();

        // Verify artifacts exist for each TFM after build.
        foreach (string tfm in targetFrameworks)
        {
            string outputPath = GetMultiTargetOutputPath(project, tfm);
            string extensionProjectDir = project.GetFunctionsExtensionProjectDirectory(tfm);

            File.Exists(Path.Combine(outputPath, "worker.config.json")).Should().BeTrue($"worker.config.json should exist in {tfm} output after build");
            Directory.Exists(Path.Combine(outputPath, Constants.ExtensionsOutputFolder)).Should().BeTrue($".azurefunctions should exist in {tfm} output after build");
            Directory.Exists(extensionProjectDir).Should().BeTrue($"extension project directory should exist for {tfm} after build");
        }

        // Act
        BuildOutput cleanOutput = project.Clean();

        // Assert
        cleanOutput.Should().BeSuccessful();
        foreach (string tfm in targetFrameworks)
        {
            string outputPath = GetMultiTargetOutputPath(project, tfm);
            string extensionProjectDir = project.GetFunctionsExtensionProjectDirectory(tfm);

            Directory.Exists(extensionProjectDir).Should().BeFalse($"extension project directory should be cleaned for {tfm}");
            Directory.Exists(Path.Combine(outputPath, Constants.ExtensionsOutputFolder)).Should().BeFalse($".azurefunctions should be cleaned from {tfm} output");
        }
    }
}
