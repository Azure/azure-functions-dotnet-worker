// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Abstractions.TestingHelpers;
using System.Text;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Azure.Functions.Sdk.Tasks.Extensions.Tests;

public sealed class WriteExtensionProjectTests
{
    private const string ProjectPath = "test/output.g.csproj";
    private const string HashPath = "test/output.g.hash";
    private readonly Mock<IBuildEngine> _buildEngine = new();
    private readonly Mock<TimeProvider> _timeProvider = new();
    private readonly MockFileSystem _fileSystem = new();
    private readonly DateTimeOffset _fixedTime = DateTimeOffset.UtcNow;

    public WriteExtensionProjectTests()
    {
        _timeProvider.Setup(x => x.GetUtcNow()).Returns(_fixedTime);
        
        // Setup mock file system with directories
        _fileSystem.AddDirectory("test");
    }

    [Fact]
    public void NoPackages_CreatesEmptyProject()
    {
        // Arrange
        WriteExtensionProject task = CreateTask(ProjectPath);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        _fileSystem.File.Exists(ProjectPath).Should().BeTrue();

        string content = _fileSystem.File.ReadAllText(ProjectPath);
        _fileSystem.AllFiles.Should().HaveCount(1); // no hash file written
        ValidateProject(content, []);
    }

    [Fact]
    public void WithPackages_CreatesProjectWithPackageReferences()
    {
        // Arrange
        TaskItem package1 = CreatePackage("Microsoft.Azure.Functions.Worker", "1.0.0");
        TaskItem package2 = CreatePackage("Microsoft.Azure.Functions.Worker.Extensions.Http", "3.0.13");
        WriteExtensionProject task = CreateTask(ProjectPath, packages: [package1, package2]);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        _fileSystem.File.Exists(ProjectPath).Should().BeTrue();

        string content = _fileSystem.File.ReadAllText(ProjectPath);
        _fileSystem.AllFiles.Should().HaveCount(1); // no hash file written
        ValidateProject(content, [package1, package2]);
    }

    [Fact]
    public void ProjectFileExists_DeletesAndRecreatesFile()
    {
        // Arrange
        _fileSystem.AddFile(ProjectPath, new MockFileData("existing content"));

        TaskItem package = CreatePackage("TestPackage", "1.0.0");
        WriteExtensionProject task = CreateTask(ProjectPath, packages: [package]);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        _fileSystem.File.Exists(ProjectPath).Should().BeTrue();

        string content = _fileSystem.File.ReadAllText(ProjectPath);
        ValidateProject(content, [package]);
    }

    [Fact]
    public void WithHashFile_UpdatesHashFile()
    {
        // Arrange
        TaskItem package = CreatePackage("TestPackage", "1.0.0");
        WriteExtensionProject task = CreateTask(ProjectPath, HashPath, [package]);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        _fileSystem.File.Exists(ProjectPath).Should().BeTrue();
        _fileSystem.File.Exists(HashPath).Should().BeTrue();

        string hash = _fileSystem.File.ReadAllText(HashPath);
        hash.Should().NotBeNullOrEmpty();

        string content = _fileSystem.File.ReadAllText(ProjectPath);
        ValidateProject(content, [package]);
    }

    [Fact]
    public void SameHashExists_SkipsGeneration()
    {
        // Arrange
        TaskItem package = CreatePackage("TestPackage", "1.0.0");

        // First execution to generate hash
        WriteExtensionProject task = CreateTask(ProjectPath, HashPath, packages: [package]);
        task.Execute();

        DateTime projectTime = _fileSystem.File.GetLastWriteTimeUtc(ProjectPath);
        DateTime hashTime = _fileSystem.File.GetLastWriteTimeUtc(HashPath);

        // Second execution with same parameters
        task = CreateTask(ProjectPath, HashPath, packages: [package]);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        _fileSystem.File.GetLastWriteTimeUtc(ProjectPath).Should().Be(projectTime);
        _fileSystem.File.GetLastWriteTimeUtc(HashPath).Should().Be(hashTime);
    }

    [Fact]
    public void DifferentHashExists_RegeneratesProject()
    {
        // Arrange
        TaskItem package1 = CreatePackage("TestPackage", "1.0.0");
        TaskItem package2 = CreatePackage("TestPackage", "2.0.0");

        // First execution
        WriteExtensionProject task = CreateTask(ProjectPath, HashPath, packages: [package1]);
        task.Execute();

        string originalHash = _fileSystem.File.ReadAllText(HashPath);
        DateTime projectTime = _fileSystem.File.GetLastWriteTimeUtc(ProjectPath);
        DateTime hashTime = _fileSystem.File.GetLastWriteTimeUtc(HashPath);

        // Second execution with different package version
        task = CreateTask(ProjectPath, HashPath, packages: [package2]);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        string currentHash = _fileSystem.File.ReadAllText(HashPath);
        string projectContent = _fileSystem.File.ReadAllText(ProjectPath);

        currentHash.Should().NotBe(originalHash);
        _fileSystem.File.GetLastWriteTimeUtc(ProjectPath).Should().BeAfter(projectTime);
        _fileSystem.File.GetLastWriteTimeUtc(HashPath).Should().BeAfter(hashTime);
        ValidateProject(projectContent, [package2]);
    }

    [Fact]
    public void CreatesFileWithUtf8Encoding()
    {
        // Arrange
        const string ProjectPath = @"C:\test\project.csproj";
        var package = CreatePackage("TestPackage", "1.0.0");
        var task = CreateTask(ProjectPath, packages: [package]);

        // Act
        bool result = task.Execute();

        // Assert
        result.Should().BeTrue();
        
        // Verify file can be read as UTF-8
        byte[] bytes = _fileSystem.File.ReadAllBytes(ProjectPath);
        string content = Encoding.UTF8.GetString(bytes);
        content.Should().Contain("<Project Sdk=");
    }

    private static void ValidateProject(string content, TaskItem[] packages)
    {
        XmlDocument doc = new();
        Action act = () => doc.LoadXml(content);
        act.Should().NotThrow();

        doc.DocumentElement.Should().NotBeNull()
            .And.HaveAttribute("Sdk", ThisAssembly.Name);

        doc.SelectNodes("/Project/PropertyGroup")!.Count.Should().Be(0);

        int i = 0;
        XmlNodeList nodes = doc.SelectNodes("/Project/ItemGroup/PackageReference")!;
        nodes.Count.Should().Be(packages.Length);
        foreach (XmlElement node in nodes)
        {
            node.Should().HaveAttribute("Include", packages[i].ItemSpec);
            node.Should().HaveAttribute("Version", packages[i].GetVersion());
            i++;
        }
    }

    private static TaskItem CreatePackage(string id, string version)
    {
        TaskItem item = new(id);
        item.SetVersion(version);
        return item;
    }

    private WriteExtensionProject CreateTask(
        string ProjectPath = @"C:\test\project.csproj",
        string? hashFilePath = null,
        ITaskItem[]? packages = null)
    {
        return new WriteExtensionProject(_fileSystem, _timeProvider.Object)
        {
            BuildEngine = _buildEngine.Object,
            ProjectPath = ProjectPath,
            HashFilePath = hashFilePath,
            ExtensionPackages = packages ?? []
        };
    }
}
