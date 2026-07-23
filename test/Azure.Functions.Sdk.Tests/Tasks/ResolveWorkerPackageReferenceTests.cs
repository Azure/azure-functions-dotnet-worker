// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Azure.Functions.Sdk.Tests;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Azure.Functions.Sdk.Tasks.Tests;

public sealed class ResolveWorkerPackageReferenceTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly Lazy<ProjectCollection> _collection = new(() => TestHelpers.CreateBinaryLoggerCollection());

    public void Dispose()
    {
        if (_collection.IsValueCreated)
        {
            _collection.Value.Dispose();
        }

        _temp.Dispose();
    }

    [Fact]
    public void LockFileDoesNotExist_EmitsWarning_AndReturnsNoPackage()
    {
        // Arrange
        Mock<IBuildEngine> buildEngine = new();
        ResolveWorkerPackageReference task = CreateTask("C:/missing/project.assets.json", buildEngine.Object, new MockFileSystem());

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        task.WorkerPackages.Should().BeEmpty();
        buildEngine.VerifyLog(LogMessage.Warning_WorkerPackageNotReferenced);
    }

    [Fact]
    public void WorkerPackageMissing_EmitsWarning_AndReturnsNoPackage()
    {
        // Arrange
        string assetsFile = RestoreProject(includeWorkerPackage: false);
        Mock<IBuildEngine> buildEngine = new();
        ResolveWorkerPackageReference task = CreateTask(assetsFile, buildEngine.Object);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        task.WorkerPackages.Should().BeEmpty();
        buildEngine.VerifyLog(LogMessage.Warning_WorkerPackageNotReferenced);
    }

    [Fact]
    public void WorkerPackagePresent_ReturnsResolvedPackage_AndNoWarning()
    {
        // Arrange
        string assetsFile = RestoreProject(includeWorkerPackage: true);
        Mock<IBuildEngine> buildEngine = new();
        ResolveWorkerPackageReference task = CreateTask(assetsFile, buildEngine.Object);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        task.WorkerPackages.Should().Contain(x =>
            string.Equals(x.ItemSpec, NugetPackage.Worker.Name, StringComparison.OrdinalIgnoreCase));

        buildEngine.Verify(m => m.LogWarningEvent(It.IsAny<BuildWarningEventArgs>()), Times.Never);
    }

    [Fact]
    public void WorkerPackagePresent_MultiTarget_ResolvesCurrentTfmOnly()
    {
        // Arrange
        string assetsFile = RestoreProject(
            includeWorkerPackage: true,
            targetFramework: null,
            configure: project => project.TargetFrameworks(["net8.0", "net481"]));

        Mock<IBuildEngine> buildEngine = new();
        ResolveWorkerPackageReference task = CreateTask(assetsFile, buildEngine.Object);
        task.TargetFramework = "net8.0";

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        task.WorkerPackages.Should().ContainSingle();
        task.WorkerPackages[0].ItemSpec.Should().Be(NugetPackage.Worker.Name);
        task.WorkerPackages[0].GetMetadata("TargetFramework").Should().Be("net8.0");
        buildEngine.Verify(m => m.LogWarningEvent(It.IsAny<BuildWarningEventArgs>()), Times.Never);
    }

    private static ResolveWorkerPackageReference CreateTask(
        string assetsFile,
        IBuildEngine buildEngine,
        IFileSystem? fileSystem = null)
    {
        return new ResolveWorkerPackageReference(fileSystem ?? new FileSystem())
        {
            BuildEngine = buildEngine,
            ProjectAssetsFile = assetsFile,
        };
    }

    private string RestoreProject(
        bool includeWorkerPackage,
        string? targetFramework = "net8.0",
        Action<ProjectCreator>? configure = null)
    {
        ProjectCreator project = ProjectCreator.Templates.AzureFunctionsProject(
            path: _temp.GetRandomCsproj(),
            targetFramework: targetFramework,
            projectCollection: _collection.Value,
            includeWorkerPackage: includeWorkerPackage)
            .CustomAction(configure)
            .WriteSourceFile("Program.cs", Resources.Program_Minimal_cs);

        project.Restore().Should().BeSuccessful();
        project.TryGetPropertyValue("ProjectAssetsFile", out string? value);
        value.Should().NotBeNullOrEmpty();
        return value!;
    }
}
