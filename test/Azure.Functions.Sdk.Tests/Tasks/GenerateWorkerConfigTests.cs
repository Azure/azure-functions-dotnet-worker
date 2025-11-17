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
        string expectedJson = WorkerConfigHelper.GetExpectedJson("dotnet", "MyFunctionApp.dll");
        fileSystem.GetFile("worker.config.json").TextContents.Should().Be(expectedJson);
    }
}
