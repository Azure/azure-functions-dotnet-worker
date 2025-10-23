// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Azure.Functions.Sdk.Tasks.Extensions.Tests;

public sealed class ValidateExtensionPackagesTests
{
    private readonly Mock<IBuildEngine> _buildEngine = new();

    [Fact]
    public void NoPackages()
    {
        // Arrange
        ValidateExtensionPackages task = CreateTask();

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        _buildEngine.VerifyNoOtherCalls();
    }

    [Fact]
    public void InvalidVersion_Error()
    {
        // Arrange
        ITaskItem package1 = CreatePackage("PackageA", "NotAValidVersion");
        ValidateExtensionPackages task = CreateTask(package1);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeFalse();
        task.FilteredPackages.Should().BeEmpty();
        _buildEngine.VerifyLog(LogMessage.Error_InvalidExtensionPackageVersion, "PackageA", "NotAValidVersion");
    }

    [Fact]
    public void DuplicatePackage_Warning()
    {
        // Arrange
        ITaskItem package1 = CreatePackage("PackageA", "1.0.0");
        ValidateExtensionPackages task = CreateTask(package1, package1);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        task.FilteredPackages.Should().BeEquivalentTo([package1]);
        _buildEngine.VerifyLog(LogMessage.Warning_ExtensionPackageDuplicate, "PackageA", "1.0.0");
    }

    [Fact]
    public void ConflictingPackage_Error()
    {
        // Arrange
        ITaskItem package1 = CreatePackage("PackageA", "1.0.0");
        ITaskItem package2 = CreatePackage("PackageA", "1.1.0");
        ValidateExtensionPackages task = CreateTask(package1, package2);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeFalse();
        task.FilteredPackages.Should().BeEquivalentTo([package1]);
        _buildEngine.VerifyLog(LogMessage.Error_ExtensionPackageConflict, "PackageA", "1.0.0", "1.1.0");
    }

    [Fact]
    public void UniquePackages_Added()
    {
        // Arrange
        ITaskItem package1 = CreatePackage("PackageA", "1.0.0");
        ITaskItem package2 = CreatePackage("PackageB", "2.0.0");
        ValidateExtensionPackages task = CreateTask(package1, package2);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        task.FilteredPackages.Should().BeEquivalentTo([package1, package2]);
        _buildEngine.VerifyNoOtherCalls();
    }

    private static TaskItem CreatePackage(string id, string version)
    {
        TaskItem item = new(id);
        item.SetVersion(version);
        return item;
    }

    private ValidateExtensionPackages CreateTask(params ITaskItem[] packages)
    {
        return new()
        {
            BuildEngine = _buildEngine.Object,
            ExtensionPackages = packages
        };
    }
}
