// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Compression;
using Azure.Functions.Sdk.Tests;
using Microsoft.Build.Framework;
using NuGet.Common;

namespace Azure.Functions.Sdk.Tasks.Publish.Tests;

public sealed class CreateZipFileTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    private readonly Mock<IBuildEngine> _buildEngine = new();

    public CreateZipFileTests()
    {
        FolderToZip = _temp.GetRandomFile();
        IntermediatePath = _temp.GetRandomFile();
        ProjectName = _temp.GetRandomFile();
    }

    private string FolderToZip { get; set; }

    private string ProjectName { get; set; }

    private string IntermediatePath { get; set; }

    public void Dispose()
    {
        _temp.Dispose();
    }

    [Fact]
    public void FolderToZipNotRooted_Error()
    {
        // arrange
        FolderToZip = "not-rooted";
        CreateZipFile task = CreateTask();

        // act
        bool result = task.Execute();

        // assert
        result.Should().BeFalse();
        _buildEngine.VerifyLog(
            LogLevel.Error,
            Strings.Zip_PathNotRooted,
            "not-rooted",
            nameof(CreateZipFile.SourceFolder));
    }

    [Fact]
    public void PublishIntermediateTempPathNotRooted_Error()
    {
        // arrange
        IntermediatePath = "not-rooted-2";
        CreateZipFile task = CreateTask();

        // act
        bool result = task.Execute();

        // assert
        result.Should().BeFalse();
        _buildEngine.VerifyLog(
            LogLevel.Error,
            Strings.Zip_PathNotRooted,
            "not-rooted-2",
            nameof(CreateZipFile.PublishIntermediateTempPath));
    }

    [Fact]
    public void CreatesZipFile_NoPermissionsChange()
    {
        // arrange
        SetupZipFolder();
        CreateZipFile task = CreateTask();

        // act
        bool result = task.Execute();

        // assert
        result.Should().BeTrue();
        VerifyZipFile(task.CreatedZipPath);
    }

    [Fact]
    public void CreatesZipFile_NoExe_PermissionsChange()
    {
        // arrange
        const string exe = "myapp";
        SetupZipFolder();
        File.WriteAllText(Path.Combine(FolderToZip, exe), string.Empty);

        CreateZipFile task = CreateTask($"{{WorkerRoot}}{exe}");

        // act
        bool result = task.Execute();

        // assert
        result.Should().BeTrue();
        VerifyZipFile(task.CreatedZipPath, exe);
    }

    [Theory]
    [InlineData("myapp.exe")]
    [InlineData("myapp")]
    public void CreatesZipFile_PermissionsChange(string name)
    {
        // arrange
        SetupZipFolder();
        File.WriteAllText(Path.Combine(FolderToZip, name), string.Empty);

        CreateZipFile task = CreateTask($"{{WorkerRoot}}myapp");

        // act
        bool result = task.Execute();

        // assert
        result.Should().BeTrue();
        VerifyZipFile(task.CreatedZipPath, name);
    }

    private CreateZipFile CreateTask(string? executable  = null)
    {
        executable ??= "dotnet";
        return new CreateZipFile()
        {
            BuildEngine = _buildEngine.Object,
            SourceFolder = FolderToZip,
            ZipName = ProjectName,
            PublishIntermediateTempPath = IntermediatePath,
            Executable = executable,
        };
    }

    private void SetupZipFolder()
    {
        Directory.CreateDirectory(FolderToZip);
        File.WriteAllText(Path.Combine(FolderToZip, Path.GetRandomFileName()), string.Empty);
    }

    private static void VerifyZipFile(string zipFile, string? executable = null)
    {
        File.Exists(zipFile).Should().BeTrue();
        using ZipArchive archive = ZipFile.OpenRead(zipFile);

        if (executable is string e)
        {
            ZipArchiveEntry entry = archive.GetEntry(e)!;
            entry.Should().NotBeNull();

            int externalAttributes = entry.ExternalAttributes;
            int unixPermissions = (externalAttributes >> 16) & 0xFFFF;
            unixPermissions.Should().Be(CreateZipFile.UnixExecutablePermissions);
        }
    }
}
