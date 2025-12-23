// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tests.Integration;

public partial class SdkEndToEndTests
{
    private static readonly string[] MinExpectedExtensionFiles =
    [
        "function.deps.json",
        "Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.dll",
    ];

    private static readonly string[] ExpectedExtensionFiles =
    [
        "Azure.Core.Amqp.dll",
        "Azure.Core.dll",
        "Azure.Identity.dll",
        "Azure.Messaging.ServiceBus.dll",
        "Azure.Storage.Blobs.dll",
        "Azure.Storage.Common.dll",
        "Azure.Storage.Queues.dll",
        "function.deps.json",
        "Google.Protobuf.dll",
        "Grpc.AspNetCore.Server.ClientFactory.dll",
        "Grpc.AspNetCore.Server.dll",
        "Grpc.Core.Api.dll",
        "Grpc.Net.Client.dll",
        "Grpc.Net.ClientFactory.dll",
        "Grpc.Net.Common.dll",
        "Microsoft.Azure.Amqp.dll",
        "Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader.dll",
        "Microsoft.Azure.WebJobs.Extensions.Rpc.dll",
        "Microsoft.Azure.WebJobs.Extensions.ServiceBus.dll",
        "Microsoft.Azure.WebJobs.Extensions.Storage.Blobs.dll",
        "Microsoft.Azure.WebJobs.Extensions.Storage.Queues.dll",
        "Microsoft.Bcl.AsyncInterfaces.dll",
        "Microsoft.Extensions.Azure.dll",
        "Microsoft.Identity.Client.dll",
        "Microsoft.Identity.Client.Extensions.Msal.dll",
        "Microsoft.IdentityModel.Abstractions.dll",
        "System.ClientModel.dll",
        "System.IO.Hashing.dll",
    ];

    private static readonly WebJobsExtension[] ExpectedExtensions =
    [
        WebJobsExtension.MetadataLoader,
        ..NugetPackage.ServiceBus.WebJobsPackages,
        ..NugetPackage.Storage.WebJobsPackages,
    ];

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
        ValidateExtensionsPayload(outputPath, MinExpectedExtensionFiles);
        ValidateExtensionJson(outputPath, WebJobsExtension.MetadataLoader);
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
        ValidateExtensionsPayload(outputPath, MinExpectedExtensionFiles);
        ValidateExtensionJson(outputPath, WebJobsExtension.MetadataLoader);
    }

    [Fact]
    public void Build_NetCore_WithExtensions()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(), targetFramework: "net8.0")
            .Property("AssemblyName", "MyFunctionApp")
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs)
            .ItemPackageReference(NugetPackage.ServiceBus)
            .ItemPackageReference(NugetPackage.Storage);

        // Act
        BuildOutput output = project.Build(restore: true);

        // Assert
        output.Should().BeSuccessful().And.HaveNoIssues();
        string outputPath = project.GetOutputPath();
        ValidateConfig(
            Path.Combine(outputPath, "worker.config.json"),
            "dotnet",
            "MyFunctionApp.dll");

        ValidateExtensionsPayload(outputPath, ExpectedExtensionFiles);
        ValidateExtensionJson(outputPath, ExpectedExtensions);
    }

    [Fact]
    public void Build_NetFx_WithExtensions()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(), targetFramework: "net481")
            .Property("AssemblyName", "MyFunctionApp")
            .Property("LangVersion", "latest")
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs)
            .ItemPackageReference(NugetPackage.ServiceBus)
            .ItemPackageReference(NugetPackage.Storage);

        // Act
        BuildOutput output = project.Build(restore: true);

        // Assert
        output.Should().BeSuccessful().And.HaveNoIssues();
        string outputPath = project.GetOutputPath();
        ValidateConfig(
            Path.Combine(outputPath, "worker.config.json"),
            "{WorkerRoot}MyFunctionApp.exe",
            "MyFunctionApp.exe");

        ValidateExtensionsPayload(outputPath, ExpectedExtensionFiles);
        ValidateExtensionJson(outputPath, ExpectedExtensions);
    }

    [Fact]
    public void Build_NoRestoreHook_Warning()
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
        ValidateExtensionsPayload(outputPath, MinExpectedExtensionFiles);
        ValidateExtensionJson(outputPath, WebJobsExtension.MetadataLoader);
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

        FileInfo config = new(configPath);
        DateTime configWriteTime = config.LastWriteTimeUtc;

        FileInfo deps = new(Path.Combine(outputPath, "function.deps.json"));
        DateTime depsWriteTime = deps.LastWriteTimeUtc;

        FileInfo extJson = new(Path.Combine(outputPath, "extensions.json"));
        DateTime extJsonWriteTime = extJson.LastWriteTimeUtc;

        // Act 2: Incremental build
        BuildOutput output2 = project.Build();

        // Assert 2: Verify no changes were made
        ValidateConfig(configPath, "dotnet", "MyFunctionApp.dll");
        ValidateExtensionsPayload(outputPath, MinExpectedExtensionFiles);
        ValidateExtensionJson(outputPath, WebJobsExtension.MetadataLoader);

        output2.Should().BeSuccessful().And.HaveNoIssues();
        config.Refresh();
        config.LastWriteTimeUtc.Should().Be(configWriteTime);

        deps.Refresh();
        deps.LastWriteTimeUtc.Should().Be(depsWriteTime);

        extJson.Refresh();
        extJson.LastWriteTimeUtc.Should().Be(extJsonWriteTime);
    }

    [Fact]
    public void Build_Incremental_WithExtensions_NoOp()
    {
        // Arrange
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            GetTempCsproj(), targetFramework: "net8.0")
            .Property("AssemblyName", "MyFunctionApp")
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs)
            .ItemPackageReference(NugetPackage.ServiceBus)
            .ItemPackageReference(NugetPackage.Storage);

        // Act
        BuildOutput output = project.Build(restore: true);

        // Assert
        output.Should().BeSuccessful().And.HaveNoIssues();
        string outputPath = project.GetOutputPath();

        string configPath = Path.Combine(outputPath, "worker.config.json");

        FileInfo config = new(configPath);
        DateTime configWriteTime = config.LastWriteTimeUtc;

        FileInfo deps = new(Path.Combine(outputPath, "function.deps.json"));
        DateTime depsWriteTime = deps.LastWriteTimeUtc;

        FileInfo extJson = new(Path.Combine(outputPath, "extensions.json"));
        DateTime extJsonWriteTime = extJson.LastWriteTimeUtc;

        // Act 2: Incremental build
        BuildOutput output2 = project.Build();

        // Assert 2: Verify no changes were made
        ValidateConfig(configPath, "dotnet", "MyFunctionApp.dll");
        ValidateExtensionsPayload(outputPath, ExpectedExtensionFiles);
        ValidateExtensionJson(outputPath, ExpectedExtensions);

        output2.Should().BeSuccessful().And.HaveNoIssues();
        config.Refresh();
        config.LastWriteTimeUtc.Should().Be(configWriteTime);

        deps.Refresh();
        deps.LastWriteTimeUtc.Should().Be(depsWriteTime);

        extJson.Refresh();
        extJson.LastWriteTimeUtc.Should().Be(extJsonWriteTime);
    }

    private static void ValidateExtensionsPayload(string outputPath, params string[] expectedFiles)
    {
        // Validate files.
        string extensionsFolder = Path.Combine(outputPath, Constants.ExtensionsOutputFolder);
        Directory.Exists(extensionsFolder).Should().BeTrue("Extensions folder should exist.");

        HashSet<string> actualFiles = Directory.GetFiles(extensionsFolder, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(extensionsFolder, f))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        actualFiles.Should().BeEquivalentTo(expectedFiles);
    }

    private static void ValidateExtensionJson(string outputPath, params WebJobsExtension[] expectedPackages)
    {
        string extensionsJsonPath = Path.Combine(outputPath, "extensions.json");
        File.Exists(extensionsJsonPath).Should().BeTrue("extensions.json should exist.");
        string extensionsJson = File.ReadAllText(extensionsJsonPath);

        WebJobsExtensions? metadata = JsonSerializer.Deserialize<WebJobsExtensions>(extensionsJson);
        metadata.Should().NotBeNull("extensions.json should deserialize correctly.");

        metadata!.Extensions.Should().HaveCount(expectedPackages.Length);
        foreach (WebJobsExtension ext in expectedPackages)
        {
            metadata.Extensions.Should().ContainSingle(e =>
                string.Equals(e.Name, ext.Name, StringComparison.OrdinalIgnoreCase))
                .Which.HintPath.Should().Be(
                    $"./{Constants.ExtensionsOutputFolder}/{ext.Assembly}",
                    "Hint path should point to correct location.");
        }
    }

    private class WebJobsExtensions
    {
        [JsonPropertyName("extensions")]
        public List<Extension> Extensions { get; set; } = [];

        public class Extension
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("hintPath")]
            public string HintPath { get; set; } = string.Empty;
        }
    }
}
