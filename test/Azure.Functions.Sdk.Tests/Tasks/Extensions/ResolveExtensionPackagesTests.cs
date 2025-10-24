// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Azure.Functions.Sdk.Tests;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tasks.Extensions.Tests;

public sealed class ResolveExtensionPackagesTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void LockFileDoesNotExist_Fails()
    {
        // Arrange
        ResolveExtensionPackages task = CreateTask(_temp.Path, new MockFileSystem());

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeFalse();
        task.ExtensionPackages.Should().BeEmpty();
    }

    [Fact]
    public void WhenCancelledBeforeProcessing_Fails()
    {
        // Arrange
        Mock<IFileSystem> fileSystem = new(MockBehavior.Strict);
        ResolveExtensionPackages task = CreateTask(_temp.Path, fileSystem.Object);

        task.Cancel();

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeFalse();
        fileSystem.VerifyNoOtherCalls();
    }

    [Fact]
    public void NoPackageRefs_Empty()
    {
        // Arrange
        string restore = RestoreProject();
        ResolveExtensionPackages task = CreateTask(restore);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        task.ExtensionPackages.Should().BeEmpty();
    }

    [Fact]
    public void NonExtensionPackages_Empty()
    {
        // Arrange
        string restore = RestoreProject(project =>
        {
            project.ItemPackageReference("System.Text.Json", "8.0.6");
        });

        ResolveExtensionPackages task = CreateTask(restore);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        task.ExtensionPackages.Should().BeEmpty();
    }

    [Fact]
    public void SinglePackage_ReturnsExtensionPackage()
    {
        // Arrange
        string restore = RestoreProject(project =>
        {
            project.ItemPackageReference(NugetPackage.ServiceBus);
        });

        ResolveExtensionPackages task = CreateTask(restore);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        task.ExtensionPackages.Should().ContainSingle();
        ValidatePackage(task.ExtensionPackages[0], NugetPackage.ServiceBus);
    }

    [Fact]
    public void MultiplePackages_ReturnsExtensionPackages()
    {
        // Arrange
        string restore = RestoreProject(project =>
        {
            project.ItemPackageReference(NugetPackage.ServiceBus);
            project.ItemPackageReference(NugetPackage.Storage);
        });

        ResolveExtensionPackages task = CreateTask(restore);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        task.ExtensionPackages.Should().HaveCount(3);

        ValidatePackage(task.ExtensionPackages[0], NugetPackage.ServiceBus);
        ValidatePackage(task.ExtensionPackages[1], NugetPackage.StorageBlobs);
        ValidatePackage(task.ExtensionPackages[2], NugetPackage.StorageQueues);
    }

    [Fact]
    public void Dispose_CancelsTokenSource()
    {
        MockFileSystem fileSystem = new();
        ResolveExtensionPackages task = new(fileSystem);

        // No direct assertion, but Dispose should not throw
        task.Dispose();
    }

    private static void ValidatePackage(ITaskItem package, WorkerPackage worker)
    {
        NugetPackage webJobs = worker.WebJobsPackages.Should().ContainSingle().Which;
        package.Should().HaveItemSpec(webJobs.Name)
            .And.HaveMetadata("Version", webJobs.Version)
            .And.HaveMetadata("SourcePackageId", worker.Name)
            .And.HaveMetadata("IsImplicitlyDefined", "true");
    }

    private static ResolveExtensionPackages CreateTask(
        string assetsFile, IFileSystem? fileSystem = null)
    {
        return new ResolveExtensionPackages(fileSystem ?? new FileSystem())
        {
            BuildEngine = Mock.Of<IBuildEngine>(),
            ProjectAssetsFile = assetsFile
        };
    }

    private string RestoreProject(Action<ProjectCreator>? configure = null)
    {
        _temp.WriteNugetConfig();
        ProjectCreator project = ProjectCreator.Templates.NetCoreProject(
            path: _temp.GetRandomFile(ext: ".csproj"), targetFramework: "net8.0", configure: configure);

        project.Restore().Should().BeSuccessful(); // use assertion to throw on failure.
        project.TryGetPropertyValue("ProjectAssetsFile", out string? value);
        value.Should().NotBeNullOrEmpty();
        return value!;
    }
}
