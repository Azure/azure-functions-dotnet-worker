// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Azure.Functions.Sdk.Tests;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities.ProjectCreation;
using Moq;

namespace Azure.Functions.Sdk.Tasks.Extensions.Tests;

public sealed class ResolveExtensionPackagesTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Execute_ReturnsFalse_WhenLockFileDoesNotExist()
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
    public void Execute_ReturnsFalse_WhenCancelledBeforeProcessing()
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
    public void Execute_ReturnsExtensionPackages_NoPackageRefs()
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
    public void Execute_ReturnsExtensionPackages_NonExtensionPackages()
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
    public void Execute_ReturnsExtensionPackages_SinglePackage()
    {
        // Arrange
        string restore = RestoreProject(project =>
        {
            project.ItemPackageReference("Microsoft.Azure.Functions.Worker.Extensions.ServiceBus", "5.23.0");
        });

        ResolveExtensionPackages task = CreateTask(restore);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        task.ExtensionPackages.Should().ContainSingle();

        ValidatePackage(
            task.ExtensionPackages[0],
            "Microsoft.Azure.WebJobs.Extensions.ServiceBus",
            "5.17.0",
            "Microsoft.Azure.Functions.Worker.Extensions.ServiceBus");
    }

    [Fact]
    public void Execute_ReturnsExtensionPackages_MultiplePackages()
    {
        // Arrange
        string restore = RestoreProject(project =>
        {
            project.ItemPackageReference("Microsoft.Azure.Functions.Worker.Extensions.ServiceBus", "5.23.0");
            project.ItemPackageReference("Microsoft.Azure.Functions.Worker.Extensions.Storage", "6.8.0");
        });

        ResolveExtensionPackages task = CreateTask(restore);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();   
        task.ExtensionPackages.Should().HaveCount(3);

        ValidatePackage(
            task.ExtensionPackages[0],
            "Microsoft.Azure.WebJobs.Extensions.ServiceBus",
            "5.17.0",
            "Microsoft.Azure.Functions.Worker.Extensions.ServiceBus");

        ValidatePackage(
            task.ExtensionPackages[1],
            "Microsoft.Azure.WebJobs.Extensions.Storage.Blobs",
            "5.3.6",
            "Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs");

        ValidatePackage(
            task.ExtensionPackages[2],
            "Microsoft.Azure.WebJobs.Extensions.Storage.Queues",
            "5.3.6",
            "Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues");
    }

    [Fact]
    public void Dispose_CancelsTokenSource()
    {
        MockFileSystem fileSystem = new();
        ResolveExtensionPackages task = new(fileSystem);

        // No direct assertion, but Dispose should not throw
        task.Dispose();
    }

    private static void ValidatePackage(
        ITaskItem package, string expectedId, string expectedVersion, string expectedSourceId)
    {
        package.Should().HaveItemSpec(expectedId)
            .And.HaveMetadata("Version", expectedVersion)
            .And.HaveMetadata("SourcePackageId", expectedSourceId)
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
