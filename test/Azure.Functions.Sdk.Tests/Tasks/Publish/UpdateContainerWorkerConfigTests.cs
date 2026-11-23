// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using Azure.Functions.Sdk.Tests;
using Microsoft.Build.Framework;
using NuGet.Common;

namespace Azure.Functions.Sdk.Tasks.Publish.Tests;

public sealed class UpdateContainerWorkerConfigTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly Mock<IBuildEngine> _buildEngine = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void UsesPrivateAppHostWithoutAddingTheDllAsAnArgument()
    {
        string stagingPath = CreateStagingDirectory("""
            { "description": {
              "defaultExecutablePath": "dotnet",
              "defaultWorkerPath": "bin/app.dll",
              "customSetting": "preserved"
            }, "other": true }
            """);
        Directory.CreateDirectory(Path.Combine(stagingPath, "bin"));
        File.WriteAllText(Path.Combine(stagingPath, "bin", "app.dll"), "assembly");
        UpdateContainerWorkerConfig task = CreateTask(stagingPath);

        task.Execute().Should().BeTrue();

        task.UsePrivateAppHost.Should().BeTrue();
        task.WorkerPath.Should().Be("bin/app.dll");
        task.WorkerAssemblyPath.Should().Be(Path.Combine(stagingPath, "bin", "app.dll"));
        using JsonDocument config = JsonDocument.Parse(File.ReadAllText(Path.Combine(stagingPath, "worker.config.json")));
        JsonElement description = config.RootElement.GetProperty("description");
        description.GetProperty("defaultExecutablePath").GetString().Should().Be("{WorkerRoot}.functions-worker");
        description.GetProperty("defaultWorkerPath").ValueKind.Should().Be(JsonValueKind.Null);
        description.GetProperty("customSetting").GetString().Should().Be("preserved");
        config.RootElement.GetProperty("other").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void PreservesCustomWorkerExecutable()
    {
        const string config = """
            { "description": { "defaultExecutablePath": "{WorkerRoot}custom", "defaultWorkerPath": "app.dll" } }
            """;
        string stagingPath = CreateStagingDirectory(config);
        UpdateContainerWorkerConfig task = CreateTask(stagingPath);

        task.Execute().Should().BeTrue();

        task.UsePrivateAppHost.Should().BeFalse();
        File.ReadAllText(Path.Combine(stagingPath, "worker.config.json")).Should().Be(config);
    }

    [Fact]
    public void RejectsWorkerAssemblyOutsidePublishDirectory()
    {
        const string config = """
            { "description": { "defaultExecutablePath": "dotnet", "defaultWorkerPath": "../outside.dll" } }
            """;
        string stagingPath = CreateStagingDirectory(config);
        File.WriteAllText(Path.Combine(_temp.Path, "outside.dll"), "assembly");
        UpdateContainerWorkerConfig task = CreateTask(stagingPath);

        task.Execute().Should().BeFalse();

        string configPath = Path.Combine(stagingPath, "worker.config.json");
        File.ReadAllText(configPath).Should().Be(config);
        _buildEngine.VerifyLog(LogLevel.Error, Strings.Container_InvalidWorkerConfig, configPath);
    }

    [Fact]
    public void RejectsTraversalToCaseVariantOfStagingDirectory()
    {
        string stagingPath = CreateStagingDirectory("""
            { "description": { "defaultExecutablePath": "dotnet", "defaultWorkerPath": "../STAGE/app.dll" } }
            """);
        string siblingPath = Path.Combine(_temp.Path, "STAGE");
        Directory.CreateDirectory(siblingPath);
        File.WriteAllText(Path.Combine(siblingPath, "app.dll"), "assembly");
        UpdateContainerWorkerConfig task = CreateTask(stagingPath);

        task.Execute().Should().BeFalse();

        _buildEngine.VerifyLog(LogLevel.Error, Strings.Container_InvalidWorkerConfig, Path.Combine(stagingPath, "worker.config.json"));
    }

    [Fact]
    public void NormalizesWorkerPathsWithinTheStagingDirectory()
    {
        string stagingPath = CreateStagingDirectory("""
            { "description": { "defaultExecutablePath": "dotnet", "defaultWorkerPath": "bin/../app.dll" } }
            """);
        File.WriteAllText(Path.Combine(stagingPath, "app.dll"), "assembly");
        UpdateContainerWorkerConfig task = CreateTask(stagingPath);

        task.Execute().Should().BeTrue();

        task.WorkerPath.Should().Be("app.dll");
        task.WorkerAssemblyPath.Should().Be(Path.Combine(stagingPath, "app.dll"));
    }

    private string CreateStagingDirectory(string config)
    {
        string path = Path.Combine(_temp.Path, "stage") + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "worker.config.json"), config);
        return path;
    }

    private UpdateContainerWorkerConfig CreateTask(string stagingPath) => new()
    {
        BuildEngine = _buildEngine.Object,
        StagingPath = stagingPath,
    };
}
