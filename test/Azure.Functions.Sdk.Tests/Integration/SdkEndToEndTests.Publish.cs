// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
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
    public void Publish_StandaloneContainerMode_IncludesWorkerConfig()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(), targetFramework: "net8.0")
            .Property("AssemblyName", "MyFunctionApp")
            .Property("FunctionsContainerMode", "Standalone")
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs);

        // Act
        BuildOutput output = project.Publish(build: true, restore: true);

        // Assert
        output.Should().BeSuccessful().And.HaveNoIssues();
        string publishPath = project.GetPublishPath();
        File.Exists(Path.Combine(publishPath, "worker.config.json")).Should().BeTrue();
        File.Exists(Path.Combine(project.GetOutputPath(), "worker.config.json")).Should().BeTrue();
        ValidateExtensionsPayload(publishPath, MinExpectedExtensionFiles);
    }

    [Fact]
    public void Target_StandaloneContainerPublish_RemovesWorkerConfig()
    {
        string publishPath = Path.Combine(TestRootPath, "publish");
        Directory.CreateDirectory(publishPath);
        string workerConfigPath = Path.Combine(publishPath, "worker.config.json");
        File.WriteAllText(workerConfigPath, "{}");

        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(GetTempCsproj())
            .Property("FunctionsContainerMode", "Standalone")
            .Property("PublishDir", publishPath + Path.DirectorySeparatorChar);

        project.TryBuild("_RemoveWorkerConfigFromStandaloneContainer", null, out _, out BuildOutput output, out _);

        output.Should().BeSuccessful().And.HaveNoIssues();
        File.Exists(workerConfigPath).Should().BeFalse();
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

    [Fact]
    public void Publish_ZipDeploy_UsesFunctionsTask()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(), targetFramework: "net8.0")
            .Property("AssemblyName", "MyFunctionApp")
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs);
        Dictionary<string, string> publishProperties = new()
        {
            ["PublishProtocol"] = "ZipDeploy",
            ["PublishUrl"] = "not-an-absolute-url",
            ["UserName"] = "test-user",
            ["Password"] = "test-password",
        };

        // Act
        BuildOutput output = project.Publish(build: true, restore: true, publishProperties);

        // Assert
        output.Should().BeFailed();
        output.ErrorEvents.Should().ContainSingle()
            .Which.Message.Should().Be(
                "The publish url 'not-an-absolute-url' is invalid. Publish url must be an absolute HTTP or HTTPS url.");
    }

    [Theory]
    [InlineData("HostEmbedded")]
    [InlineData("Standalone")]
    public void Target_ContainerConfig_UsesModeDefaultsAndSdkVersion(string mode)
    {
        const string consumerVersion = "987.654.321-consumer";
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(), targetFramework: "net8.0")
            .Property("FunctionsContainerMode", mode)
            .Property("AssemblyName", "ContainerApp")
            .Property("Version", consumerVersion)
            .Property("PackageVersion", consumerVersion)
            .Property("_SdkVersionDuringEvaluation", "$(AzureFunctionsSdkVersion)");

        ITaskItem[] items = GetContainerConfig(project);

        GetContainerProperty(items, "ContainerRuntimeIdentifier").Should().Be("linux-x64");
        ITaskItem label = items.Should().ContainSingle(x => x.ItemSpec == "com.azure.functions.sdk").Which;
        project.TryGetPropertyValue("AzureFunctionsSdkVersion", out string sdkVersion);
        sdkVersion.Should().NotBeNullOrWhiteSpace().And.NotBe(consumerVersion);
        project.TryGetPropertyValue("_SdkVersionDuringEvaluation", out string evaluatedSdkVersion);
        evaluatedSdkVersion.Should().Be(sdkVersion);
        label.GetMetadata("Value").Should().Be(sdkVersion);

        if (mode == "HostEmbedded")
        {
            GetContainerProperty(items, "ContainerBaseImage")
                .Should().Be("mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0");
            GetContainerProperty(items, "ContainerWorkingDirectory").Should().Be("/home/site/wwwroot");
            items.Should().ContainSingle(x => x.ItemSpec == "/opt/startup/start_nonappservice.sh");
            items.Should().ContainSingle(x => x.ItemSpec == "AzureWebJobsScriptRoot")
                .Which.GetMetadata("Value").Should().Be("/home/site/wwwroot");
            items.Should().ContainSingle(x => x.ItemSpec == "AzureFunctionsJobHost__Logging__Console__IsEnabled")
                .Which.GetMetadata("Value").Should().Be("true");
        }
        else
        {
#if NET
            GetContainerProperty(items, "ContainerBaseImage").Should().Be("mcr.microsoft.com/dotnet/runtime:8.0");
            GetContainerProperty(items, "ContainerWorkingDirectory").Should().Be("/app/");
            items.Should().ContainSingle(x => x.ItemSpec == "dotnet");
            items.Should().ContainSingle(x => x.ItemSpec == "/app/ContainerApp.dll");
#else
            GetContainerProperty(items, "ContainerBaseImage").Should().BeEmpty();
            GetContainerProperty(items, "ContainerWorkingDirectory").Should().BeEmpty();
#endif
            items.Should().NotContain(x => x.ItemSpec == "AzureWebJobsScriptRoot");
            items.Should().NotContain(x => x.ItemSpec == "AzureFunctionsJobHost__Logging__Console__IsEnabled");
        }
    }

    [Theory]
    [InlineData("HostEmbedded")]
    [InlineData("Standalone")]
    public void Target_ContainerConfig_PreservesExplicitSettings(string mode)
    {
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(), targetFramework: "net8.0")
            .Property("FunctionsContainerMode", mode)
            .Property("ContainerBaseImage", "example.com/custom:latest")
            .Property("ContainerRuntimeIdentifier", "linux-arm64")
            .Property("ContainerWorkingDirectory", "/custom/");
        ProjectItemElement environmentVariable = project.RootElement.AddItem(
            "ContainerEnvironmentVariable", "AzureWebJobsScriptRoot");
        environmentVariable.AddMetadata("Value", "/existing");

        ITaskItem[] items = GetContainerConfig(project);

        GetContainerProperty(items, "ContainerBaseImage").Should().Be("example.com/custom:latest");
        GetContainerProperty(items, "ContainerRuntimeIdentifier").Should().Be("linux-arm64");
        GetContainerProperty(items, "ContainerWorkingDirectory").Should().Be("/custom/");
        items.Should().ContainSingle(x => x.ItemSpec == "AzureWebJobsScriptRoot")
            .Which.GetMetadata("Value").Should().Be("/existing");
    }

    [Fact]
    public void Target_ContainerConfig_InvalidModeFails()
    {
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(GetTempCsproj())
            .Property("FunctionsContainerMode", "Invalid");

        project.TryBuild("ComputeContainerBaseImage", null, out _, out BuildOutput output, out _);

        output.Should().BeFailed().And.HaveSingleError()
            .Which.Message.Should().Be(
                "Invalid FunctionsContainerMode 'Invalid'. Supported values are 'HostEmbedded' and 'Standalone'.");
    }

    [Fact]
    public void Target_ContainerConfig_EmptySdkVersionAddsEmptyLabel()
    {
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(),
            globalProperties: new Dictionary<string, string>
            {
                ["AzureFunctionsSdkVersion"] = string.Empty,
            });

        ITaskItem[] items = GetContainerConfig(project);

        items.Should().ContainSingle(x => x.ItemSpec == "com.azure.functions.sdk")
            .Which.GetMetadata("Value").Should().BeEmpty();
    }

    private static ITaskItem[] GetContainerConfig(ProjectCreator project)
    {
        ProjectTargetElement target = project.RootElement.AddTarget("_TestContainerConfig");
#if NET
        target.DependsOnTargets = "ComputeContainerConfig";
#else
        // The .NET container tasks cannot be hosted by the full-framework in-process test runner.
        target.DependsOnTargets = "ComputeFunctionsContainerConfig";
#endif
        target.Returns = "@(_ContainerTestProperty);@(ContainerLabel);@(ContainerAppCommand);@(ContainerEnvironmentVariable)";

        ProjectItemGroupElement properties = target.AddItemGroup();
        foreach (string name in new[] { "ContainerBaseImage", "ContainerRuntimeIdentifier", "ContainerWorkingDirectory" })
        {
            properties.AddItem("_ContainerTestProperty", name).AddMetadata("Value", $"$({name})");
        }

        project.RootElement.Save();
        project.TryBuild(target.Name, null, out _, out BuildOutput output, out IDictionary<string, TargetResult>? results);
        output.Should().BeSuccessful().And.HaveNoIssues();
        return results![target.Name].Items;
    }

    private static string GetContainerProperty(ITaskItem[] items, string name) =>
        items.Should().ContainSingle(x => x.ItemSpec == name).Which.GetMetadata("Value");
}
