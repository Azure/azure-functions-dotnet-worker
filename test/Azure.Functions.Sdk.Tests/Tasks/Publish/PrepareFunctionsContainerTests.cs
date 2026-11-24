// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Functions.Sdk.Tests;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Common;

namespace Azure.Functions.Sdk.Tasks.Publish.Tests;

public sealed class PrepareFunctionsContainerTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly Mock<IBuildEngine> _buildEngine = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void UsesLatestCompatibleRuntimePatches()
    {
        PrepareFunctionsContainer task = CreateTask("""
            { "runtimeOptions": { "frameworks": [
              { "name": "Microsoft.NETCore.App", "version": "11.0.0" },
              { "name": "Microsoft.AspNetCore.App", "version": "11.0.0" }
            ] } }
            """);

        task.Execute().Should().BeTrue();

        task.CoreRuntimeVersion.Should().Be("11.0.5");
        task.AspNetRuntimeVersion.Should().Be("11.0.5");
        task.PublishedPath.Should().Be(Path.Combine(_temp.Path, "publish") + Path.DirectorySeparatorChar);
        task.StagingPath.Should().StartWith(Path.Combine(_temp.Path, "obj") + Path.DirectorySeparatorChar);
        Directory.Exists(task.StagingRoot).Should().BeFalse();
    }

    [Theory]
    [InlineData("\"rollForward\": \"Disable\"")]
    [InlineData("\"applyPatches\": false")]
    public void PinnedRuntimeConfigurationUsesRequestedVersions(string policy)
    {
        PrepareFunctionsContainer task = CreateTask($$"""
            { "runtimeOptions": { {{policy}}, "frameworks": [
              { "name": "Microsoft.NETCore.App", "version": "11.0.0" },
              { "name": "Microsoft.AspNetCore.App", "version": "11.0.0" }
            ] } }
            """);

        task.Execute().Should().BeTrue();

        task.CoreRuntimeVersion.Should().Be("11.0.0");
        task.AspNetRuntimeVersion.Should().Be("11.0.0");
    }

    [Fact]
    public void NewerRequestedVersionIsNotDowngradedToSdkPatch()
    {
        PrepareFunctionsContainer task = CreateTask("""
            { "runtimeOptions": { "framework": { "name": "Microsoft.NETCore.App", "version": "11.0.7" } } }
            """);

        task.Execute().Should().BeTrue();

        task.CoreRuntimeVersion.Should().Be("11.0.7");
        task.AspNetRuntimeVersion.Should().BeEmpty();
    }

    [Fact]
    public void DoesNotRollStableRuntimeToPreview()
    {
        PrepareFunctionsContainer task = CreateTask("""
            { "runtimeOptions": { "framework": { "name": "Microsoft.NETCore.App", "version": "11.0.0" } } }
            """, latestVersion: "11.0.1-preview.1");

        task.Execute().Should().BeTrue();

        task.CoreRuntimeVersion.Should().Be("11.0.0");
    }

    [Fact]
    public void AlignsAspNetRuntimeWithPinnedCoreRuntime()
    {
        PrepareFunctionsContainer task = CreateTask("""
            { "runtimeOptions": { "frameworks": [
              { "name": "Microsoft.NETCore.App", "version": "11.0.0", "rollForward": "Disable" },
              { "name": "Microsoft.AspNetCore.App", "version": "11.0.0" }
            ] } }
            """);

        task.Execute().Should().BeTrue();

        task.CoreRuntimeVersion.Should().Be("11.0.0");
        task.AspNetRuntimeVersion.Should().Be("11.0.0");
    }

    [Fact]
    public void NewerAspNetRuntimeRaisesCompatibleCoreRuntime()
    {
        PrepareFunctionsContainer task = CreateTask("""
            { "runtimeOptions": { "frameworks": [
              { "name": "Microsoft.NETCore.App", "version": "11.0.0" },
              { "name": "Microsoft.AspNetCore.App", "version": "11.0.7" }
            ] } }
            """);

        task.Execute().Should().BeTrue();

        task.CoreRuntimeVersion.Should().Be("11.0.7");
        task.AspNetRuntimeVersion.Should().Be("11.0.7");
    }

    [Fact]
    public void IncompatiblePinnedFrameworksFail()
    {
        PrepareFunctionsContainer task = CreateTask("""
            { "runtimeOptions": { "rollForward": "Disable", "frameworks": [
              { "name": "Microsoft.NETCore.App", "version": "11.0.0" },
              { "name": "Microsoft.AspNetCore.App", "version": "11.0.1" }
            ] } }
            """);

        task.Execute().Should().BeFalse();

        _buildEngine.VerifyLog(LogLevel.Error, Strings.Container_IncompatibleFrameworks, "11.0.0", "11.0.1");
    }

    [Fact]
    public void UnsupportedFrameworkFails()
    {
        PrepareFunctionsContainer task = CreateTask("""
            { "runtimeOptions": { "frameworks": [
              { "name": "Microsoft.NETCore.App", "version": "11.0.0" },
              { "name": "Custom.Framework", "version": "11.0.0" }
            ] } }
            """);

        task.Execute().Should().BeFalse();

        _buildEngine.VerifyLog(LogLevel.Error, Strings.Container_UnsupportedFramework, "Custom.Framework", "11.0.0");
    }

    [Fact]
    public void OverlappingPublishAndIntermediatePathsFail()
    {
        PrepareFunctionsContainer task = CreateTask("{}");
        task.PublishDirectory = _temp.Path;

        task.Execute().Should().BeFalse();

        _buildEngine.VerifyLog(LogLevel.Error, Strings.Container_OverlappingPaths, task.PublishedPath, task.StagingRoot);
        Directory.Exists(task.StagingRoot).Should().BeFalse();
    }

    [Fact]
    public void UsesKnownFrameworksForProjectTargetFramework()
    {
        PrepareFunctionsContainer task = CreateTask("""
            { "runtimeOptions": { "framework": { "name": "Microsoft.NETCore.App", "version": "12.0.0" } } }
            """);
        task.TargetFramework = "net12.0";
        task.KnownFrameworkReferences = [.. task.KnownFrameworkReferences, KnownFramework("Microsoft.NETCore.App", "12.0.3", "net12.0")];

        task.Execute().Should().BeTrue();

        task.CoreRuntimeVersion.Should().Be("12.0.3");
    }

    private PrepareFunctionsContainer CreateTask(string runtimeConfig, string latestVersion = "11.0.5")
    {
        string publishPath = Path.Combine(_temp.Path, "publish");
        Directory.CreateDirectory(publishPath);
        File.WriteAllText(Path.Combine(publishPath, "app.runtimeconfig.json"), runtimeConfig);

        return new PrepareFunctionsContainer
        {
            BuildEngine = _buildEngine.Object,
            ProjectDirectory = _temp.Path,
            PublishDirectory = "publish",
            IntermediateOutputPath = "obj",
            RuntimeConfigFileName = "app.runtimeconfig.json",
            TargetFramework = "net11.0",
            KnownFrameworkReferences =
            [
                KnownFramework("Microsoft.NETCore.App", latestVersion),
                KnownFramework("Microsoft.AspNetCore.App", latestVersion),
            ],
        };
    }

    private static ITaskItem KnownFramework(string name, string version, string targetFramework = "net11.0")
    {
        TaskItem item = new(name);
        item.SetMetadata("TargetFramework", targetFramework);
        item.SetMetadata("LatestRuntimeFrameworkVersion", version);
        return item;
    }
}
