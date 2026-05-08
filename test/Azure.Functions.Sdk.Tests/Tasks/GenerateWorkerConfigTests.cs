// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Abstractions.TestingHelpers;
using Microsoft.Build.Framework;

namespace Azure.Functions.Sdk.Tasks.Tests;

public sealed class GenerateWorkerConfigTests
{
    [Fact]
    public void WritesConfigFile()
    {
        // arrange
        MockFileSystem fileSystem = new();
        GenerateWorkerConfig task = new(fileSystem)
        {
            Executable = "dotnet",
            EntryPoint = "MyFunctionApp.dll",
            OutputPath = "worker.config.json",
            BuildEngine = Mock.Of<IBuildEngine>(),
        };

        // act
        bool result = task.Execute();

        // assert
        result.Should().BeTrue();
        fileSystem.FileExists("worker.config.json").Should().BeTrue();
        string expectedJson = ExpectedFilesHelper.GetWorkerConfig("dotnet", "MyFunctionApp.dll");
        fileSystem.GetFile("worker.config.json").TextContents.Should().Be(expectedJson);
    }

    [Fact]
    public void SkipsWriteWhenContentUnchanged()
    {
        // arrange
        string expectedJson = ExpectedFilesHelper.GetWorkerConfig("dotnet", "MyFunctionApp.dll");
        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            ["worker.config.json"] = new MockFileData(expectedJson),
        });

        DateTime originalWriteTime = fileSystem.File.GetLastWriteTimeUtc("worker.config.json");

        GenerateWorkerConfig task = new(fileSystem)
        {
            Executable = "dotnet",
            EntryPoint = "MyFunctionApp.dll",
            OutputPath = "worker.config.json",
            BuildEngine = Mock.Of<IBuildEngine>(),
        };

        // act
        bool result = task.Execute();

        // assert
        result.Should().BeTrue();
        fileSystem.GetFile("worker.config.json").TextContents.Should().Be(expectedJson);
        fileSystem.File.GetLastWriteTimeUtc("worker.config.json").Should().Be(originalWriteTime);
    }

    [Fact]
    public void OverwritesWhenContentChanged()
    {
        // arrange
        string oldJson = ExpectedFilesHelper.GetWorkerConfig("dotnet", "OldApp.dll");
        MockFileSystem fileSystem = new(new Dictionary<string, MockFileData>
        {
            ["worker.config.json"] = new MockFileData(oldJson),
        });

        GenerateWorkerConfig task = new(fileSystem)
        {
            Executable = "{WorkerRoot}MyFunctionApp.exe",
            EntryPoint = "MyFunctionApp.dll",
            OutputPath = "worker.config.json",
            BuildEngine = Mock.Of<IBuildEngine>(),
        };

        // act
        bool result = task.Execute();

        // assert
        result.Should().BeTrue();
        string expectedJson = ExpectedFilesHelper.GetWorkerConfig("{WorkerRoot}MyFunctionApp.exe", "MyFunctionApp.dll");
        fileSystem.GetFile("worker.config.json").TextContents.Should().Be(expectedJson);
    }
}
