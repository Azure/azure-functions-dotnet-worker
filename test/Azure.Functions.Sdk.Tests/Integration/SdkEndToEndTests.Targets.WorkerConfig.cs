// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests.Integration;

public partial class SdkEndToEndTests : MSBuildSdkTestBase
{
    [Fact]
    public void Target_ResolveFunctionExecutable_Default()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj());

        // Act
        project.Restore().Should().BeSuccessful();
        TargetResult? result = project.RunTarget("_ResolveFunctionExecutable");

        // Assert
        result.Should().NotBeNull();
        result!.ResultCode.Should().Be(TargetResultCode.Success);
        result.Items.Should().ContainSingle()
            .Which.ItemSpec.Should().Be("dotnet");
    }

    [Theory]
    [InlineData("win-x64")]
    [InlineData("linux-x64")]
    public void Target_ResolveFunctionExecutable_SelfContained(string runtime)
    {
        // Arrange
        Dictionary<string, string> globalProperties = new()
        {
            ["SelfContained"] = "true",
            ["RuntimeIdentifier"] = runtime,
        };

        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj())
            .Property("AssemblyName", "MyFunctionApp");

        // Act
        project.Restore().Should().BeSuccessful();
        TargetResult? result = project.RunTarget("_ResolveFunctionExecutable", globalProperties);

        // Assert
        string expected = runtime.StartsWith("win") ? "MyFunctionApp.exe" : "MyFunctionApp";
        result.Should().NotBeNull();
        result!.ResultCode.Should().Be(TargetResultCode.Success);
        result.Items.Should().ContainSingle()
            .Which.ItemSpec.Should().Be($"{{WorkerRoot}}{expected}");
    }

    [Fact]
    public void Target_ResolveFunctionExecutable_NetFx()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(), targetFramework: "net481")
            .Property("AssemblyName", "MyFunctionApp");

        // Act
        project.Restore().Should().BeSuccessful();
        TargetResult? result = project.RunTarget("_ResolveFunctionExecutable");

        // Assert
        result.Should().NotBeNull();
        result!.ResultCode.Should().Be(TargetResultCode.Success);
        result.Items.Should().ContainSingle()
            .Which.ItemSpec.Should().Be("{WorkerRoot}MyFunctionApp.exe");
    }

    [Fact]
    public void Target_PreGenerateWorkerConfig()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj());

        // Act
        // We want partial IntermediateOutputPath.
        project.Restore().Should().BeSuccessful();
        TargetResult? result = project.RunTarget("_PreGenerateWorkerConfig");

        // Assert
        result.Should().NotBeNull();
        result!.ResultCode.Should().Be(TargetResultCode.Success);
        result.Items.Should().ContainSingle()
            .Which.Should().Satisfy<ITaskItem>(item =>
            {
                item.ItemSpec.Should().Be($"{project.GetRelativeIntermediateOutputPath()}worker.config.json");
                item.GetMetadata("TargetPath").Should().Be("worker.config.json");
            });
    }

    [Theory]
    [InlineData("DesignTimeBuild")]
    [InlineData("NoBuild")]
    public void Target_GenerateWorkerConfig_Skipped_NoOp(string condition)
    {
        // Arrange
        Dictionary<string, string> globalProperties = new()
        {
            [condition] = "true",
        };

        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj());

        // Act
        project.Restore().Should().BeSuccessful();
        TargetResult? result = project.RunTarget("_GenerateWorkerConfig", globalProperties);

        // Assert
        result.Should().NotBeNull();
        result!.ResultCode.Should().Be(TargetResultCode.Skipped);
        File.Exists(Path.Combine(
            project.GetIntermediateOutputPath(),
            "worker.config.json")).Should().BeFalse();
    }

    [Fact]
    public void Target_GenerateWorkerConfig_Default()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj())
            .Property("AssemblyName", "MyFunctionApp")
            .CreateIntermediateOutputPath();

        // Act
        project.Restore().Should().BeSuccessful();
        TargetResult? result = project.RunTarget("_GenerateWorkerConfig");

        // Assert
        string expectedPath = Path.Combine(project.GetIntermediateOutputPath(), "worker.config.json");
        result.Should().NotBeNull();
        result!.ResultCode.Should().Be(TargetResultCode.Success);
        ValidateConfig(expectedPath, "dotnet", "MyFunctionApp.dll");
    }

    [Theory]
    [InlineData("win-x64")]
    [InlineData("linux-x64")]
    public void Target_GenerateWorkerConfig_SelfContained(string runtime)
    {
        // Arrange
        Dictionary<string, string> globalProperties = new()
        {
            ["SelfContained"] = "true",
            ["RuntimeIdentifier"] = runtime,
        };

        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj())
            .Property("AssemblyName", "MyFunctionApp")
            .CreateIntermediateOutputPath(runtime);

        // Act
        project.Restore().Should().BeSuccessful();
        TargetResult? result = project.RunTarget("_GenerateWorkerConfig", globalProperties);

        // Assert
        string expectedExe = runtime.StartsWith("win") ? "{WorkerRoot}MyFunctionApp.exe" : "{WorkerRoot}MyFunctionApp";
        string expectedPath = Path.Combine(project.GetIntermediateOutputPath(runtime), "worker.config.json");
        result.Should().NotBeNull();
        result!.ResultCode.Should().Be(TargetResultCode.Success);
        ValidateConfig(expectedPath, expectedExe, "MyFunctionApp.dll");
    }

    [Fact]
    public void Target_GenerateWorkerConfig_NetFx()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(), targetFramework: "net481")
            .Property("AssemblyName", "MyFunctionApp")
            .CreateIntermediateOutputPath();

        // Act
        project.Restore().Should().BeSuccessful();
        TargetResult? result = project.RunTarget("_GenerateWorkerConfig");

        // Assert
        string expectedPath = Path.Combine(project.GetIntermediateOutputPath(), "worker.config.json");
        result.Should().NotBeNull();
        result!.ResultCode.Should().Be(TargetResultCode.Success);
        ValidateConfig(expectedPath, "{WorkerRoot}MyFunctionApp.exe", "MyFunctionApp.exe");
    }

    private static void ValidateConfig(string file, string expectedExecutable, string expectedEntryPoint)
    {
        File.Exists(file).Should().BeTrue();
        string expectedJson = ExpectedFilesHelper.GetWorkerConfig(expectedExecutable, expectedEntryPoint);
        File.ReadAllText(file).Should().Be(expectedJson);
    }
}
